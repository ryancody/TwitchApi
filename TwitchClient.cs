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
    private readonly string channelName;
    private readonly string applicationId;
    private User broadcasterUser;
    private DeviceCodeResponse deviceCodeResponse;

    private bool isTokenValid = false;

    private const int shortValidationTimer = 2000;

    public TwitchClient(AppInfo appInfo)
    {
        webSocket = new ClientWebSocket();
        httpClient = new TwitchHttpClient(appInfo);
        channelName = appInfo.Channel;
        applicationId = appInfo.Id;
        httpClient.RequestProcessed += OnRequestProcessed;
    }

    private string GetGlobalizedAuthPath()
    {
        var dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(dir, $"TwitchApi-{applicationId}", "twitch_auth.json");
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
                        broadcasterUser = (await httpClient.GetUsers(authToken.AccessToken, logins: [channelName])).Data.First();

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

    private async Task<AuthInfo> GetAuthToken()
    {
        if (File.Exists(GetGlobalizedAuthPath()))
        {
            var localJson = File.ReadAllText(GetGlobalizedAuthPath());

            try
            {
                var cachedAuthInfo = JsonSerializer.Deserialize<AuthInfo>(localJson);

                if (cachedAuthInfo is not null)
                {
                    if (cachedAuthInfo.ExpirationDate > DateTimeOffset.UtcNow.AddMinutes(5))
                        return cachedAuthInfo;
                    else
                    {
                        Console.WriteLine("Refreshing auth token");

                        var refreshedAuthInfo = await httpClient.RefreshAuthToken(deviceCodeResponse.DeviceCode, cachedAuthInfo.RefreshToken);
                        var refreshedToken = new AuthInfo(refreshedAuthInfo.AccessToken, refreshedAuthInfo.RefreshToken, deviceCodeResponse.DeviceCode, refreshedAuthInfo.ExpiresIn);

                        SaveAuthInfo(refreshedToken);

                        return refreshedToken;
                    }
                }
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

        var token = new AuthInfo(tokenResponse.AccessToken, tokenResponse.RefreshToken, deviceCodeResponse.DeviceCode, tokenResponse.ExpiresIn);

        SaveAuthInfo(token);

        return token;
    }

    private void SaveAuthInfo(AuthInfo authInfo)
    {
        Console.WriteLine($"Saving auth info to {GetGlobalizedAuthPath()}");

        var authJson = JsonSerializer.Serialize(authInfo);

        Directory.CreateDirectory(Path.GetDirectoryName(GetGlobalizedAuthPath())!);

        File.WriteAllText(GetGlobalizedAuthPath(), authJson);
    }
}
