using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TwitchApi.Models;
using TwitchApi.Models.Responses;
using HttpClient = System.Net.Http.HttpClient;

namespace TwitchApi;

public class TwitchHttpClient : HttpClient
{
    internal Action<HttpRequestMessage, TwitchResponse> RequestProcessed;

    private AppInfo clientInfo;
    private const string scopes = "user:read:chat channel:read:subscriptions";
    private readonly Dictionary<string, BroadcasterSubsciptionResponse> broadcasterSubscriptions = [];

    public TwitchHttpClient(AppInfo clientInfo) : base()
    {
        ArgumentNullException.ThrowIfNull(clientInfo, nameof(clientInfo));

        this.clientInfo = clientInfo;
    }

    internal Task<DeviceCodeResponse> GetDeviceCode()
    {
        var httpRequest = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri("https://id.twitch.tv/oauth2/device?"),
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientInfo.Id,
                ["scopes"] = scopes
            })
        };

        return SendRequestAsync<DeviceCodeResponse>(httpRequest);
    }

    internal Task<TokenResponse> GetTokenResponse(string deviceCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceCode);

        var httpRequest = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri("https://id.twitch.tv/oauth2/token?"),
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientInfo.Id,
                ["scopes"] = scopes,
                ["device_code"] = deviceCode,
                ["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code"
            })
        };

        return SendRequestAsync<TokenResponse>(httpRequest);
    }

    internal Task<TokenResponse> RefreshAuthToken(string deviceCode, string refreshToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceCode);

        var httpRequest = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri("https://id.twitch.tv/oauth2/token?"),
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientInfo.Id,
                ["scopes"] = scopes,
                ["device_code"] = deviceCode,
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken
            })
        };

        return SendRequestAsync<TokenResponse>(httpRequest);
    }

    internal Task<SubscribeResponse> SubscribeToChannelUpdate(string token, string broadcasterUserId, string sessionId)
    {
        var httpRequestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri("https://api.twitch.tv/helix/eventsub/subscriptions"),
            Headers =
            {
                { "Client-ID", clientInfo.Id },
                { "Authorization", $"Bearer {token}" }
            },
            Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    type = "channel.update",
                    version = "2",
                    condition = new
                    {
                        broadcaster_user_id = broadcasterUserId,
                        user_id = broadcasterUserId
                    },
                    transport = new
                    {
                        method = "websocket",
                        session_id = sessionId
                    }
                }),
                Encoding.UTF8,
                "application/json"
            )
        };

        return SendRequestAsync<SubscribeResponse>(httpRequestMessage);
    }

    internal Task<SubscribeResponse> SubscribeToChannelChatMessage(string token, string broadcasterUserId, string sessionId)
    {
        var httpRequestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri("https://api.twitch.tv/helix/eventsub/subscriptions"),
            Headers =
            {
                { "Client-ID", clientInfo.Id },
                { "Authorization", $"Bearer {token}" }
            },
            Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    type = "channel.chat.message",
                    version = "1",
                    condition = new
                    {
                        broadcaster_user_id = broadcasterUserId,
                        user_id = broadcasterUserId
                    },
                    transport = new
                    {
                        method = "websocket",
                        session_id = sessionId
                    }
                }),
                Encoding.UTF8,
                "application/json"
            )
        };
        httpRequestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        return SendRequestAsync<SubscribeResponse>(httpRequestMessage);
    }

    internal Task<UsersResponse> GetUsers(string token, string[] logins = null, string[] ids = null)
    {
        if (logins?.Length <= 0 && ids?.Length <= 0)
            throw new ArgumentException("Need at least 1 login or id to GetUsers");

        var queryParts = new List<string>();

        if (logins?.Length > 0)
            queryParts.AddRange(logins.Select(l => $"login={Uri.EscapeDataString(l)}"));

        if (ids?.Length > 0)
            queryParts.AddRange(ids.Select(id => $"id={Uri.EscapeDataString(id)}"));

        var query = string.Join("&", queryParts);
        var httpRequest = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri($"https://api.twitch.tv/helix/users?{query}"),
            Headers =
            {
                { "Client-ID", clientInfo.Id },
                { "Authorization", $"Bearer {token}" }
            }
        };

        return SendRequestAsync<UsersResponse>(httpRequest);
    }



    internal async Task<BroadcasterSubsciptionResponse> GetBroadcasterSubscriptions(string token, string broadcasterUserId, string chatterUserId)
    {
        if (broadcasterSubscriptions.TryGetValue(chatterUserId, out var subscription))
            return subscription;

        var url =
            $"https://api.twitch.tv/helix/subscriptions" +
            $"?broadcaster_id={Uri.EscapeDataString(broadcasterUserId)}" +
            $"&user_id={Uri.EscapeDataString(chatterUserId)}";

        var httpRequest = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri(url),
            Headers =
            {
                { "Client-ID", clientInfo.Id },
                { "Authorization", $"Bearer {token}" }
            }
        };

        var broadcasterSubscriptionsResponse = await SendRequestAsync<BroadcasterSubsciptionResponse>(httpRequest);

        if (broadcasterSubscriptionsResponse.Status == 404)
            broadcasterSubscriptions.Add(chatterUserId, null);

        return broadcasterSubscriptionsResponse;
    }

    internal async Task<ValidateTokenResponse> ValidateToken(string token)
    {
        var httpRequest = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri("https://id.twitch.tv/oauth2/validate"),
            Headers =
            {
                { "Authorization", $"OAuth {token}" }
            }
        };

        var validateTokenResponse = await SendRequestAsync<ValidateTokenResponse>(httpRequest);

        return validateTokenResponse;
    }

    private async Task<T> SendRequestAsync<T>(HttpRequestMessage httpRequest) where T : TwitchResponse
    {
        var result = await SendAsync(httpRequest);
        var content = await result.Content.ReadAsStringAsync();

        Console.WriteLine($"{typeof(T).Name} Response Content: " + content);

        var response = JsonSerializer.Deserialize<T>(content);

        response.IsSuccessStatusCode = result.IsSuccessStatusCode;

        RequestProcessed?.Invoke(httpRequest, response);

        return response;
    }
}
