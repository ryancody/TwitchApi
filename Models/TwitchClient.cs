using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using TwitchApi.Models;

namespace TwitchApi;

public class TwitchClient
{
    public event Action<Models.Event> MessageReceived;

    private ClientWebSocket webSocket;
    private TwitchHttpClient httpClient;
    private CancellationTokenSource cts = new CancellationTokenSource();
    private Queue<WebSocketMessage> messageQueue = [];
    private string broadcasterLogin;
    private User broadcasterUser;
    private DeviceCodeResponse deviceCodeResponse;
    private readonly string cachedBearerTokenGlobalizedPath = Path.GetFullPath("user://twitch_token.json");

    private bool isTokenValid = false;

    private const int shortValidationTimer = 2000;

    public TwitchClient(AppInfo appInfo, string channelName)
    {
        webSocket = new ClientWebSocket();
        httpClient = new TwitchHttpClient(appInfo);
        broadcasterLogin = channelName;
        httpClient.RequestProcessed += OnRequestProcessed;
    }

    private void OnRequestProcessed(HttpRequestMessage request, TwitchResponse response)
    {
        if (response.Status == 400
            && response.Message.Equals("authorization_pending", StringComparison.OrdinalIgnoreCase))
        {
            isTokenValid = false;
        }
    }

    public async Task ConnectAsync()
    {
        var token = await GetAuthToken();

        if (token is null)
        {
            deviceCodeResponse = await httpClient.GetDeviceCode();
            Console.WriteLine("Visit " + deviceCodeResponse.VerificationUri);
        }

        _ = Task.Run(QueryTokenValidation, cts.Token);
        _ = Task.Run(Receive, cts.Token);
        _ = Task.Run(ProcessMessages, cts.Token);
    }

    public async Task<bool> IsUserSubscribed(string chatterUserId)
    {
        var token = await GetAuthToken();
        var broadcasterSubsciptionResponse = await httpClient.GetBroadcasterSubscriptions(token.AccessToken, broadcasterUser.Id, chatterUserId);
        var subscriber = broadcasterSubsciptionResponse?.Data.FirstOrDefault();

        return !string.IsNullOrWhiteSpace(subscriber?.PlanName);
    }

    private async Task QueryTokenValidation()
    {
        while (!cts.IsCancellationRequested)
        {
            if (!isTokenValid)
            {
                Console.WriteLine("checking for valid token...");
                var authToken = await GetAuthToken();

                if (authToken is not null)
                {
                    var validateTokenResponse = await httpClient.ValidateToken(authToken.AccessToken);

                    if (validateTokenResponse.IsSuccessStatusCode)
                    {
                        isTokenValid = true;
                        broadcasterUser = (await httpClient.GetUsers(authToken.AccessToken, logins: [broadcasterLogin])).Data.First();

                        try
                        {
                            if (webSocket.State != WebSocketState.Open && webSocket.State != WebSocketState.Connecting)
                            {
                                Console.WriteLine("Validated token, starting websocket...");
                                var uri = new Uri("wss://eventsub.wss.twitch.tv/ws");
                                await webSocket.ConnectAsync(uri, cts.Token);
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("error starting websocket: " + e.Message);
                        }

                        Console.WriteLine("websocket: " + webSocket.State);

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
                        Console.WriteLine("Received: " + text);
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

                Console.WriteLine($"Processing message {message.Metadata.MessageId} of type {message.Metadata.MessageType}");
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

                Console.WriteLine($"Session established with ID: {sessionId}");
                Subscribe(message.Payload.Session.Id);
                break;

            case "notification":
                Console.WriteLine($"Notification received");
                ProcessNotification(message);
                break;

            case "session_keepalive":
                Console.WriteLine("keepalive received.");
                break;

            default:
                Console.WriteLine($"Unknown message type: {message.Metadata.MessageType}");
                break;
        }
    }

    private void ProcessNotification(WebSocketMessage message)
    {
        var eventData = message.Payload.Event;

        Console.WriteLine($"Event received: Broadcaster {eventData.BroadcasterUserName}, Chatter {eventData.ChatterUserName}, Message: {eventData.Message.Text}");

        MessageReceived?.Invoke(eventData);
    }

    private async void Subscribe(string sessionId)
    {
        var token = await GetAuthToken();

        _ = httpClient.SubscribeToChannelUpdate(token.AccessToken, broadcasterUser.Id, sessionId);
        _ = httpClient.SubscribeToChannelChatMessage(token.AccessToken, broadcasterUser.Id, sessionId);
    }

    private async Task<AuthToken> GetAuthToken()
    {
        if (File.Exists(cachedBearerTokenGlobalizedPath))
        {
            var localJson = File.ReadAllText(cachedBearerTokenGlobalizedPath);

            try
            {
                var localToken = JsonSerializer.Deserialize<AuthToken>(localJson);

                if (localToken?.ExpirationDate > DateTimeOffset.UtcNow.AddMinutes(5))
                    return localToken;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unable to deserialize cached token: {ex.Message}");
            }
        }

        if (deviceCodeResponse?.DeviceCode is null)
        {
            Console.WriteLine("device code missing");
            return null;
        }

        var tokenResponse = await httpClient.GetTokenResponse(deviceCodeResponse.DeviceCode);

        if (!tokenResponse.IsSuccessStatusCode)
        {
            Console.WriteLine("Message: " + tokenResponse.Message);
            return null;
        }

        var token = new AuthToken(tokenResponse.AccessToken, tokenResponse.ExpiresIn);
        var tokenJson = JsonSerializer.Serialize(token);

        File.WriteAllText(cachedBearerTokenGlobalizedPath, tokenJson);

        return token;
    }
}
