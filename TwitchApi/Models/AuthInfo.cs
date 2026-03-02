namespace TwitchApi.Models;

public class AuthInfo
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public DateTimeOffset ExpirationDate { get; set; }

    public AuthInfo(string accessToken, string refreshToken, int secondsToExpiration)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        ExpirationDate = DateTimeOffset.UtcNow.AddSeconds(secondsToExpiration);
    }

    public AuthInfo()
    {

    }
}
