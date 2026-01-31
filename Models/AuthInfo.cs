namespace TwitchApi.Models;

public class AuthInfo
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public DateTimeOffset ExpirationDate { get; set; }
    public string DeviceCode { get; set; }

    public AuthInfo(string accessToken, string refreshToken, string deviceCode, int secondsToExpiration)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        DeviceCode = deviceCode;
        ExpirationDate = DateTimeOffset.UtcNow.AddSeconds(secondsToExpiration);
    }

    public AuthInfo()
    {

    }
}
