using System.Net.WebSockets;
using Microsoft.Extensions.Logging;
using TwitchApi.Models;
using TwitchApi.Models.Responses;
using TwitchApi.Providers;
using TwitchApi.Providers.Models;

namespace TwitchApi;

public class TwitchClient
{
    public event Action<WebSocketMessage> MessageReceived;
    public event Action<ConnectionStatus> ConnectionStatusChanged;
    public event Action<string> DeviceAuthorizationRequested;
    public event Action<string> TokenValidated;
    public LoginInfo LoginInfo { get; private set; }
    public ConnectionStatus ConnectionStatus
    {
        get
        {
            return connectionStatus;
        }
        private set
        {
            if (connectionStatus != value)
            {
                connectionStatus = value;
                ConnectionStatusChanged?.Invoke(connectionStatus);
            }
        }
    }

    private ConnectionStatus connectionStatus = ConnectionStatus.Disconnected;
    private SocketWrapper webSocket;
    private TwitchHttpClient httpClient;
    private ILogger<TwitchClient> logger;
    private CancellationTokenSource cts = new CancellationTokenSource();
    private User broadcasterUser;
    private readonly string channelName;
    private volatile bool isTokenValid = false;
    private const int shortValidationTimer = 2000;
    private readonly IAuthProvider authProvider;
    private readonly string eventSubWebSocketUrl = "wss://eventsub.wss.twitch.tv/ws";
    private Task? processMessagesTask;
    private Task? queryTokenValidationTask;

    public TwitchClient(string channelName, string webSocketUrl, TwitchHttpClient httpClient, IAuthProvider authProvider, ILogger<TwitchClient> logger)
    {
        this.httpClient = httpClient;
        this.channelName = channelName;
        this.logger = logger;
        this.authProvider = authProvider;
        eventSubWebSocketUrl = webSocketUrl;
        httpClient.RequestProcessed += OnRequestProcessed;
        httpClient.LoginInfoValidated += OnLoginInfoValidated;

        httpClient.TokenValidated += token =>
        {
            ConnectionStatus = ConnectionStatus.Connected;
            TokenValidated?.Invoke(token);
        };

        if (authProvider is LocalAuthProvider localProvider)
            localProvider.DeviceAuthorizationRequested += url => DeviceAuthorizationRequested?.Invoke(url);
    }

    private void OnLoginInfoValidated(LoginInfo info) => LoginInfo = info;

    private void OnRequestProcessed(HttpRequestMessage request, TwitchResponse response)
    {
        if (response.Status == 400
            && response.Message.Equals(MessageTypes.AuthorizationPending, StringComparison.OrdinalIgnoreCase))
        {
            isTokenValid = false;
            ConnectionStatus = ConnectionStatus.Pending;
        }
    }

    public void Connect()
    {
        ConnectionStatus = ConnectionStatus.Connecting;

        cts.Cancel();
        cts.Dispose();
        cts = new CancellationTokenSource();
        isTokenValid = false;
        webSocket?.Dispose();
        webSocket = new SocketWrapper(logger);

        processMessagesTask = ProcessMessages();
        queryTokenValidationTask = QueryTokenValidation();
    }

    public async Task DisconnectAsync()
    {
        cts.Cancel();

        if (webSocket.State == WebSocketState.Open)
            await webSocket.StopReceiving();

        ConnectionStatus = ConnectionStatus.Disconnected;

        if (processMessagesTask is not null)
            await processMessagesTask;

        if (queryTokenValidationTask is not null)
            await queryTokenValidationTask;

        await authProvider.SaveAuthInfoAsync(null);
    }

    public async Task<UsersResponse> GetUsers(string[] logins = null, string[] ids = null)
    {
        var authInfo = await authProvider.GetAuthInfoAsync();

        if (authInfo is null)
            throw new Exception("No valid token available");

        return await httpClient.GetUsers(authInfo.AccessToken, logins, ids);
    }

    public async Task<bool> IsUserSubscribed(string chatterUserId)
    {
        var authInfo = await authProvider.GetAuthInfoAsync();

        if (authInfo is null)
        {
            logger.LogInformation("Unable to check subscription: No valid token available");
            return false;
        }

        var broadcasterSubsciptionResponse = await httpClient.GetBroadcasterSubscriptions(authInfo.AccessToken, broadcasterUser.Id, chatterUserId);
        var subscriber = broadcasterSubsciptionResponse?.Data.FirstOrDefault();

        return !string.IsNullOrWhiteSpace(subscriber?.PlanName);
    }

    public async Task<ValidateTokenResponse> ValidateTokenAsync(string token) =>
        await httpClient.ValidateTokenAsync(token);

    public async Task ValidateAuthInfo()
    {
        var authInfo = await authProvider.GetAuthInfoAsync();

        if (authInfo is null)
        {
            logger.LogInformation("No valid token available for validation");
            return;
        }

        await httpClient.ValidateTokenAsync(authInfo.AccessToken);
    }

    private async Task QueryTokenValidation()
    {
        while (!cts.IsCancellationRequested)
        {
            try
            {
                if (!isTokenValid)
                {
                    logger.LogInformation("checking for valid token...");
                    var authInfo = await authProvider.GetAuthInfoAsync();

                    if (authInfo is not null)
                    {
                        var validateTokenResponse = await httpClient.ValidateTokenAsync(authInfo.AccessToken);

                        if (validateTokenResponse.IsSuccessStatusCode)
                        {
                            ConnectionStatus = ConnectionStatus.Connected;
                            isTokenValid = true;
                            broadcasterUser = (await httpClient.GetUsers(authInfo.AccessToken, logins: [channelName])).Data.First();

                            if (webSocket.State != WebSocketState.Open && webSocket.State != WebSocketState.Connecting)
                            {
                                logger.LogInformation("Validated token, starting websocket...");
                                
                                await webSocket.StartReceiving(new Uri(eventSubWebSocketUrl));
                            }

                            await Task.Delay(500);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogInformation("error starting websocket: " + e.Message);
            }

            await Task.Delay(shortValidationTimer);
        }
    }

    private async Task ProcessMessages()
    {
        while (!cts.IsCancellationRequested)
        {
            while (webSocket.Messages.Count > 0)
            {
                if (webSocket.Messages.TryDequeue(out var message))
                {
                    logger.LogInformation($"Processing message {message.Metadata?.MessageId} of type {message.Metadata?.MessageType}");
                    await ProcessMessage(message);
                }
            }
            await Task.Delay(100);
        }
    }

    private async Task ProcessMessage(WebSocketMessage message)
    {
        switch (message.Metadata?.MessageType)
        {
            case MessageTypes.SessionWelcome:
                var sessionId = message.Payload.Session.Id;
                logger.LogInformation($"Session established with ID: {sessionId}");
                await Subscribe(message.Payload.Session.Id);
                break;

            case MessageTypes.Notification:
                logger.LogInformation($"Notification received");
                ProcessNotification(message);
                break;

            case MessageTypes.SessionKeepalive:
                logger.LogInformation("keepalive received.");
                ConnectionStatus = ConnectionStatus.Connected;
                break;

            case MessageTypes.SessionReconnect:
                var reconnectUrl = message.Payload.Session.ReconnectUrl;
                await HandleWebsocketReconnect(reconnectUrl);
                break;

            default:
                logger.LogInformation($"Unknown message type: {message.Metadata.MessageType}");
                break;
        }
    }

    private async Task HandleWebsocketReconnect(string reconnectUrl)
    {
        logger.LogInformation($"Reconnecting to: {reconnectUrl}");

        var newSocket = new SocketWrapper(logger);
        
        async void OnMessage(WebSocketMessage message)
        {
            logger.LogInformation($"checking for welcome message on {reconnectUrl}");
            if (message.Metadata?.MessageType != MessageTypes.SessionWelcome)
                return;

            logger.LogInformation($"Welcome message received on new socket");

            newSocket.MessageReceived -= OnMessage;

            var oldsocket = webSocket;
            webSocket = newSocket;
            await oldsocket.StopReceiving();
            oldsocket.Dispose();
        };

        newSocket.MessageReceived += OnMessage;
        await newSocket.StartReceiving(new Uri(reconnectUrl));
    }

    private void ProcessNotification(WebSocketMessage message)
    {
        MessageReceived?.Invoke(message);
    }

    private async Task<SubscribeResponse[]> Subscribe(string sessionId)
    {
        var authInfo = await authProvider.GetAuthInfoAsync();

        if (authInfo is null)
        {
            logger.LogInformation("Unable to subscribe: No valid token available");
            return []; 
        }
        
        return await Task.WhenAll(
             httpClient.Subscribe(SubscriptionTypes.ChannelUpdate, authInfo.AccessToken, broadcasterUser.Id, broadcasterUser.Id, sessionId),
             httpClient.Subscribe(SubscriptionTypes.ChannelChatMessage, authInfo.AccessToken, broadcasterUser.Id, broadcasterUser.Id, sessionId),
             httpClient.Subscribe(SubscriptionTypes.ChannelSubscribe, authInfo.AccessToken, broadcasterUser.Id, broadcasterUser.Id, sessionId)
        );
    }
}
