using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TwitchApi.Models;

namespace TwitchApi;

public class TwitchClient
{
    public event Action<Event> MessageReceived;
    public event Action<bool> ConnectionChanged;
    public event Action<string> DeviceAuthorizationRequested;
    public event Action<string> TokenChanged;
    public bool IsConnected => isTokenValid;
    public LoginInfo LoginInfo { get; private set; }

    private ClientWebSocket webSocket;
    private TwitchHttpClient httpClient;
    private ILogger<TwitchClient> logger;
    private CancellationTokenSource cts = new CancellationTokenSource();
    private Queue<WebSocketMessage> messageQueue = [];
    private User broadcasterUser;
    private DeviceCodeResponse deviceCodeResponse;
    private readonly string channelName;
    private readonly string appId;
    private bool isTokenValid = false;
    private string token = string.Empty;
    private const int shortValidationTimer = 2000;

    public TwitchClient(string channelName, string appId, ILogger<TwitchClient> logger)
    {
        webSocket = new ClientWebSocket();
        httpClient = new TwitchHttpClient(appId);
        this.channelName = channelName;
        this.appId = appId;
        this.logger = logger;
        httpClient.RequestProcessed += OnRequestProcessed;
        httpClient.LoginInfoValidated += OnLoginInfoValidated;
    }

    private void OnLoginInfoValidated(LoginInfo info) => LoginInfo = info;

    private void OnRequestProcessed(HttpRequestMessage request, TwitchResponse response)
    {
        if (response.Status == 400
            && response.Message.Equals("authorization_pending", StringComparison.OrdinalIgnoreCase))
        {
            isTokenValid = false;
            ConnectionChanged?.Invoke(isTokenValid);
        }
    }

    public async Task ConnectAsync()
    {
        var token = await GetAuthInfo();

        if (token is null)
        {
            deviceCodeResponse = await httpClient.GetDeviceCode();
            logger.LogInformation("Verify device at " + deviceCodeResponse.VerificationUri);
            DeviceAuthorizationRequested?.Invoke(deviceCodeResponse.VerificationUri);
        }

        _ = Task.Run(QueryTokenValidation, cts.Token);
        _ = Task.Run(Receive, cts.Token);
        _ = Task.Run(ProcessMessages, cts.Token);
    }

    public async Task<UsersResponse> GetUsers(string[] logins = null, string[] ids = null)
    {
        var token = await GetAuthInfo();

        if (token is null)
            throw new Exception("No valid token available");

        return await httpClient.GetUsers(token.AccessToken, logins, ids);
    }

    public async Task<bool> IsUserSubscribed(string chatterUserId)
    {
        var token = await GetAuthInfo();
        var broadcasterSubsciptionResponse = await httpClient.GetBroadcasterSubscriptions(token.AccessToken, broadcasterUser.Id, chatterUserId);
        var subscriber = broadcasterSubsciptionResponse?.Data.FirstOrDefault();

        return !string.IsNullOrWhiteSpace(subscriber?.PlanName);
    }

    public async Task<ValidateTokenResponse> ValidateTokenAsync(string token) =>
        await httpClient.ValidateTokenAsync(token);

    private async Task QueryTokenValidation()
    {
        while (!cts.IsCancellationRequested)
        {
            if (!isTokenValid)
            {
                logger.LogInformation("checking for valid token...");
                var authToken = await GetAuthInfo();

                if (authToken is not null)
                {
                    var validateTokenResponse = await httpClient.ValidateTokenAsync(authToken.AccessToken);

                    if (validateTokenResponse.IsSuccessStatusCode)
                    {
                        isTokenValid = true;
                        ConnectionChanged?.Invoke(isTokenValid);
                        TokenChanged?.Invoke(authToken.AccessToken);
                        broadcasterUser = (await httpClient.GetUsers(authToken.AccessToken, logins: [channelName])).Data.First();
                        try
                        {
                            if (webSocket.State != WebSocketState.Open && webSocket.State != WebSocketState.Connecting)
                            {
                                logger.LogInformation("Validated token, starting websocket...");
                                var uri = new Uri("wss://eventsub.wss.twitch.tv/ws");
                                await webSocket.ConnectAsync(uri, cts.Token);
                            }
                        }
                        catch (Exception e)
                        {
                            logger.LogInformation("error starting websocket: " + e.Message);
                        }

                        logger.LogInformation("websocket: " + webSocket.State);

                        await Task.Delay(1000);
                    }
                }
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
                var result = await webSocket.ReceiveAsync(buffer, cts.Token);

                sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));

                if (result.EndOfMessage)
                {
                    var text = sb.ToString();
                    sb.Clear();

                    if (!string.IsNullOrEmpty(text))
                    {
                        logger.LogInformation("Received: " + text);
                        messageQueue.Enqueue(JsonSerializer.Deserialize<WebSocketMessage>(text));
                    }
                }
            }

            await Task.Delay(100);
        }
    }

    private async Task ProcessMessages()
    {
        while (!cts.IsCancellationRequested)
        {
            while (messageQueue.Count > 0)
            {
                var message = messageQueue.Dequeue();

                logger.LogInformation($"Processing message {message.Metadata.MessageId} of type {message.Metadata.MessageType}");
                ProcessMessage(message);
            }
            await Task.Delay(100);
        }
    }

    private void ProcessMessage(WebSocketMessage message)
    {
        switch (message.Metadata.MessageType)
        {
            case "session_welcome":
                var sessionId = message.Payload.Session.Id;

                logger.LogInformation($"Session established with ID: {sessionId}");
                Subscribe(message.Payload.Session.Id);
                break;

            case "notification":
                logger.LogInformation($"Notification received");
                ProcessNotification(message);
                break;

            case "session_keepalive":
                logger.LogInformation("keepalive received.");
                break;

            default:
                logger.LogInformation($"Unknown message type: {message.Metadata.MessageType}");
                break;
        }
    }

    private void ProcessNotification(WebSocketMessage message)
    {
        var eventData = message.Payload.Event;

        logger.LogInformation($"Event received: Broadcaster {eventData.BroadcasterUserName}, Chatter {eventData.ChatterUserName}, Message: {eventData.Message.Text}");

        MessageReceived?.Invoke(eventData);
    }

    private async void Subscribe(string sessionId)
    {
        var token = await GetAuthInfo();

        _ = httpClient.SubscribeToChannelUpdate(token.AccessToken, broadcasterUser.Id, sessionId);
        _ = httpClient.SubscribeToChannelChatMessage(token.AccessToken, broadcasterUser.Id, sessionId);
    }

    private async Task<AuthInfo> GetAuthInfo()
    {
        if (File.Exists(GetGlobalizedAuthPath()))
        {
            AuthInfo cachedAuthInfo = null;

            try
            {
                var localJson = File.ReadAllText(GetGlobalizedAuthPath());
                cachedAuthInfo = JsonSerializer.Deserialize<AuthInfo>(localJson);
            }
            catch (Exception ex)
            {
                logger.LogInformation($"Unable to deserialize cached token: {ex.Message}");
            }

            if (cachedAuthInfo is not null)
            {
                if (cachedAuthInfo.ExpirationDate > DateTimeOffset.UtcNow.AddMinutes(5))
                    return cachedAuthInfo;
                else
                {
                    logger.LogInformation("Refreshing auth token");

                    var refreshedAuthInfo = await httpClient.RefreshAuthToken(cachedAuthInfo.RefreshToken);
                    var refreshedToken = new AuthInfo(refreshedAuthInfo.AccessToken, refreshedAuthInfo.RefreshToken, refreshedAuthInfo.ExpiresIn);

                    SaveAuthInfo(refreshedToken);

                    return refreshedToken;
                }
            }
        }

        if (deviceCodeResponse?.DeviceCode is null)
        {
            logger.LogInformation("Unable to get AuthInfo: Device code missing");
            return null;
        }

        var tokenResponse = await httpClient.GetTokenResponse(deviceCodeResponse.DeviceCode);

        if (!tokenResponse.IsSuccessStatusCode)
        {
            logger.LogInformation("Message: " + tokenResponse.Message);
            return null;
        }

        var token = new AuthInfo(tokenResponse.AccessToken, tokenResponse.RefreshToken, tokenResponse.ExpiresIn);

        SaveAuthInfo(token);

        return token;
    }

    private string GetGlobalizedAuthPath()
    {
        var dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(dir, $"TwitchApi-{appId}", "twitch_auth.json");
    }

    private void SaveAuthInfo(AuthInfo authInfo)
    {
        logger.LogInformation($"Saving auth info to {GetGlobalizedAuthPath()}");

        var authJson = JsonSerializer.Serialize(authInfo);

        Directory.CreateDirectory(Path.GetDirectoryName(GetGlobalizedAuthPath())!);
        File.WriteAllText(GetGlobalizedAuthPath(), authJson);
    }
}
