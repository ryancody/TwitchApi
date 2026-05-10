using System.Text.Json;
using Microsoft.Extensions.Logging;
using TwitchApi.Models;
using TwitchApi.Providers.Models;

namespace TwitchApi.Providers;

public class LocalAuthProvider : IAuthProvider
{
    public event Action<string> DeviceAuthorizationRequested;

    private string appId;
    private TwitchHttpClient httpClient;
    private ILogger logger;
    private DeviceCodeResponse deviceCodeResponse;

    public LocalAuthProvider(string appId, TwitchHttpClient httpClient, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(appId, nameof(appId));
        ArgumentNullException.ThrowIfNull(httpClient, nameof(httpClient));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));

        this.appId = appId;
        this.httpClient = httpClient;
        this.logger = logger;
    }

    public async Task<AuthInfo> GetAuthInfoAsync()
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

                    await SaveAuthInfoAsync(refreshedToken);

                    return refreshedToken;
                }
            }
        }

        if (deviceCodeResponse is null)
        {
            deviceCodeResponse = await httpClient.GetDeviceCode();
            DeviceAuthorizationRequested?.Invoke(deviceCodeResponse.VerificationUri);
        }

        var tokenResponse = await httpClient.GetTokenResponse(deviceCodeResponse.DeviceCode);

        if (!tokenResponse.IsSuccessStatusCode)
            return null;

        deviceCodeResponse = null;

        var token = new AuthInfo(tokenResponse.AccessToken, tokenResponse.RefreshToken, tokenResponse.ExpiresIn);
        await SaveAuthInfoAsync(token);
        return token;
    }

    private string GetGlobalizedAuthPath()
    {
        var dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(dir, $"TwitchApi-{appId}", "twitch_auth.json");
    }

    public async Task SaveAuthInfoAsync(AuthInfo authInfo)
    {
        logger.LogInformation($"Saving auth info to {GetGlobalizedAuthPath()}");

        var authJson = JsonSerializer.Serialize(authInfo);

        Directory.CreateDirectory(Path.GetDirectoryName(GetGlobalizedAuthPath())!);
        await File.WriteAllTextAsync(GetGlobalizedAuthPath(), authJson);
    }
}
