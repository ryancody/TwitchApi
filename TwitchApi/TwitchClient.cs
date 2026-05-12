using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TwitchApi.Models;
using TwitchApi.Providers;
using TwitchApi.Providers.Models;

namespace TwitchApi;

public class TwitchClient
{
    public event Action<Event> MessageReceived;
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
    private ClientWebSocket webSocket;
    private TwitchHttpClient httpClient;
    private ILogger<TwitchClient> logger;
    private CancellationTokenSource cts = new CancellationTokenSource();
    private ConcurrentQueue<WebSocketMessage> messageQueue = [];
    private User broadcasterUser;
    private readonly string channelName;
    private volatile bool isTokenValid = false;
    private const int shortValidationTimer = 2000;
    private readonly IAuthProvider authProvider;
    private const string eventSubWebSocketUrl = "wss://eventsub.wss.twitch.tv/ws";
    // private const string eventSubWebSocketUrl = "ws://127.0.0.1:8080/ws";

    public TwitchClient(string channelName, TwitchHttpClient httpClient, IAuthProvider authProvider, ILogger<TwitchClient> logger)
    {
        this.httpClient = httpClient;
        this.channelName = channelName;
        this.logger = logger;
        this.authProvider = authProvider;
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

    public async Task ConnectAsync()
    {
        ConnectionStatus = ConnectionStatus.Connecting;

        cts.Cancel();
        cts.Dispose();
        cts = new CancellationTokenSource();
        isTokenValid = false;
        webSocket?.Dispose();
        webSocket = new ClientWebSocket();

        _ = Task.Run(QueryTokenValidation, cts.Token);
    }

    public async Task DisconnectAsync()
    {
        cts.Cancel();

        if (webSocket.State == WebSocketState.Open)
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnecting", CancellationToken.None);

        ConnectionStatus = ConnectionStatus.Disconnected;

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
                                var uri = new Uri(eventSubWebSocketUrl);
                                await webSocket.ConnectAsync(uri, cts.Token);

                                _ = Task.Run(Receive, cts.Token);
                                _ = Task.Run(ProcessMessages, cts.Token);
                            }

                            logger.LogInformation("websocket: " + webSocket.State);

                            await Task.Delay(1000);
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

    private async Task Receive()
    {
        var buffer = new byte[8192];
        var sb = new StringBuilder();

        while (!cts.IsCancellationRequested)
        {
            if (webSocket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result;
                try
                {
                    result = await webSocket.ReceiveAsync(buffer, cts.Token);
                }
                catch (TaskCanceledException)
                {
                    // Don't care about task cancellation exceptions
                    return;
                }
                catch
                {
                    throw;
                }

                sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));

                if (result.EndOfMessage)
                {
                    var text = sb.ToString();
                    sb.Clear();

                    if (!string.IsNullOrEmpty(text))
                    {
                        logger.LogInformation("Received: " + text);

                        var message = JsonSerializer.Deserialize<WebSocketMessage>(text);

                        messageQueue.Enqueue(message);
                    }
                }
            }
        }
    }

    private async Task ProcessMessages()
    {
        while (!cts.IsCancellationRequested)
        {
            while (messageQueue.Count > 0)
            {
                if (messageQueue.TryDequeue(out var message))
                {
                    logger.LogInformation($"Processing message {message.Metadata?.MessageId} of type {message.Metadata?.MessageType}");
                    _ = ProcessMessage(message);
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
                logger.LogInformation($"Reconnecting to: {reconnectUrl}");
                
                webSocket.Dispose();
                webSocket = new ClientWebSocket();
                
                await webSocket.ConnectAsync(new Uri(reconnectUrl), cts.Token);
                break;

            default:
                logger.LogInformation($"Unknown message type: {message.Metadata.MessageType}");
                break;
        }
    }

    private void ProcessNotification(WebSocketMessage message)
    {
        var eventData = message.Payload.Event;

        logger.LogInformation($"Event received: MessageType: {message.Metadata?.MessageType ?? "empty"}, Broadcaster {eventData?.BroadcasterUserName ?? "empty"}, Chatter {eventData?.ChatterUserName ?? "empty"}, Message: {eventData?.Message?.Text ?? "empty"}");

        MessageReceived?.Invoke(eventData);
    }

    private async Task Subscribe(string sessionId)
    {
        var authInfo = await authProvider.GetAuthInfoAsync();

        if (authInfo is null)
        {
            logger.LogInformation("Unable to subscribe: No valid token available");
            return; 
        }
        
        _ = httpClient.Subscribe(SubscriptionTypes.ChannelUpdate, authInfo.AccessToken, broadcasterUser.Id, broadcasterUser.Id, sessionId);
        _ = httpClient.Subscribe(SubscriptionTypes.ChannelChatMessage, authInfo.AccessToken, broadcasterUser.Id, broadcasterUser.Id, sessionId);
        _ = httpClient.Subscribe(SubscriptionTypes.ChannelSubscribe, authInfo.AccessToken, broadcasterUser.Id, broadcasterUser.Id, sessionId);
    }
}
