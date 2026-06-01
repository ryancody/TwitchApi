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
    internal Action<LoginInfo> LoginInfoValidated;
    internal Action<string> TokenValidated;

    private readonly Dictionary<string, BroadcasterSubsciptionResponse> broadcasterSubscriptions = [];

    public TwitchHttpClient() : base()
    {}

    internal Task<DeviceCodeResponse> GetDeviceCode(string clientId, string scopes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(scopes);

        var httpRequest = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri("https://id.twitch.tv/oauth2/device?"),
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["scopes"] = scopes
            })
        };

        return SendRequestAsync<DeviceCodeResponse>(httpRequest);
    }

    internal Task<TokenResponse> GetTokenResponse(string clientId, string deviceCode, string scopes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(scopes);

        var httpRequest = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri("https://id.twitch.tv/oauth2/token?"),
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["device_code"] = deviceCode,
                ["scopes"] = scopes,
                ["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code"
            })
        };

        return SendRequestAsync<TokenResponse>(httpRequest);
    }

    internal Task<TokenResponse> RefreshTokenAsync(string clientId, string refreshToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);

        var httpRequest = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri("https://id.twitch.tv/oauth2/token?"),
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["refresh_token"] = refreshToken,
                ["grant_type"] = "refresh_token"
            })
        };

        return SendRequestAsync<TokenResponse>(httpRequest);
    }

    internal Task<TokenResponse> RefreshTokenAsync(string clientId, string clientSecret, string refreshToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientSecret);
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);

        var httpRequest = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri("https://id.twitch.tv/oauth2/token?"),
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["refresh_token"] = refreshToken,
                ["grant_type"] = "refresh_token"
            })
        };

        return SendRequestAsync<TokenResponse>(httpRequest);
    }

    internal Task<SubscribeResponse> Subscribe(string clientId, string authToken, string eventTypeName, string eventTypeVersion, string broadcasterUserId, string userId, string sessionId)
    {
        var httpRequestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri("https://api.twitch.tv/helix/eventsub/subscriptions"),
            Headers =
            {
                { "Client-ID", clientId },
                { "Authorization", $"Bearer {authToken}" }
            },
            Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    type = eventTypeName,
                    version = eventTypeVersion,
                    condition = new
                    {
                        broadcaster_user_id = broadcasterUserId,
                        user_id = userId
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

    // get info about one ore more users
    internal Task<UsersResponse> GetUsers(string clientId, string authToken, string[] logins = null, string[] ids = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(authToken);

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
                { "Client-ID", clientId },
                { "Authorization", $"Bearer {authToken}" }
            }
        };

        return SendRequestAsync<UsersResponse>(httpRequest);
    }

    internal async Task<BroadcasterSubsciptionResponse> GetBroadcasterSubscriptions(string clientId, string authToken, string broadcasterUserId, string chatterUserId)
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
                { "Client-ID", clientId },
                { "Authorization", $"Bearer {authToken}" }
            }
        };

        var broadcasterSubscriptionsResponse = await SendRequestAsync<BroadcasterSubsciptionResponse>(httpRequest);

        if (broadcasterSubscriptionsResponse.Status == 404)
            broadcasterSubscriptions.Add(chatterUserId, null);

        return broadcasterSubscriptionsResponse;
    }

    public async Task<ValidateTokenResponse> ValidateTokenAsync(string authToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(authToken);

        var httpRequest = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri("https://id.twitch.tv/oauth2/validate"),
            Headers =
            {
                { "Authorization", $"OAuth {authToken}" }
            }
        };

        var validateTokenResponse = await SendRequestAsync<ValidateTokenResponse>(httpRequest);

        LoginInfoValidated?.Invoke(new LoginInfo
        {
            Login = validateTokenResponse.Login,
            UserId = validateTokenResponse.UserId
        });

        if (validateTokenResponse.IsSuccessStatusCode) 
            TokenValidated?.Invoke(authToken);

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
