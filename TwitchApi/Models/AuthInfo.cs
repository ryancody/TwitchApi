namespace TwitchApi.Models;

public class AuthInfo
{
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public DateTimeOffset ExpirationDate { get; set; }

    public AuthInfo(string clientId, string clientSecret, string accessToken, string refreshToken, int secondsToExpiration)
    {
        ClientId = clientId;
        ClientSecret = clientSecret;
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        ExpirationDate = DateTimeOffset.UtcNow.AddSeconds(secondsToExpiration);
    }
}
