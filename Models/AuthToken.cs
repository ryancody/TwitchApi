namespace TwitchApi.Models;

public class AuthToken
{
    public string AccessToken { get; set; }
    public DateTimeOffset ExpirationDate { get; set; }
    public DateTimeOffset LastValidationDate { get; set; }

    public AuthToken(string accessToken, int secondsToExpiration)
    {
        AccessToken = accessToken;
        ExpirationDate = DateTimeOffset.UtcNow.AddSeconds(secondsToExpiration);
    }

    public AuthToken()
    {

    }
}
