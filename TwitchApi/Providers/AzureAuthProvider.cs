using Azure.Data.AppConfiguration;
using Microsoft.Extensions.Logging;
using TwitchApi.Models;
using TwitchApi.Providers.Models;

namespace TwitchApi.Providers;

public class AzureAuthProvider : IAuthProvider
{
    private readonly TwitchHttpClient httpClient;
    private readonly ILogger logger;
    private readonly ConfigurationClient configClient;
    private const string RefreshTokenKey = "TWITCH_REFRESH_TOKEN";
    private AuthInfo cachedAuthInfo;

    public AzureAuthProvider(TwitchHttpClient httpClient, ILogger logger)
    {
        this.httpClient = httpClient;
        this.logger = logger;

        var connectionString = Environment.GetEnvironmentVariable("APP_CONFIG_CONNECTION_STRING");
        configClient = new ConfigurationClient(connectionString);
    }

    public async Task<AuthInfo> GetAuthInfoAsync()
    {
        if (cachedAuthInfo is not null)
        {
            if (cachedAuthInfo.ExpirationDate > DateTimeOffset.UtcNow.AddMinutes(5))
                return cachedAuthInfo;

            return await RefreshAsync(cachedAuthInfo.RefreshToken);
        }

        var refreshToken = await GetRefreshTokenFromConfigAsync();

        if (refreshToken is null)
        {
            logger.LogError("No refresh token found in App Configuration");
            return null;
        }

        return await RefreshAsync(refreshToken);
    }

    public async Task SaveAuthInfoAsync(AuthInfo authInfo)
    {
        cachedAuthInfo = authInfo;
        await WriteRefreshTokenAsync(authInfo.RefreshToken);
    }

    private async Task<AuthInfo> RefreshAsync(string refreshToken)
    {
        logger.LogInformation("Refreshing auth token");

        var refreshed = await httpClient.RefreshAuthToken(refreshToken);
        var authInfo = new AuthInfo(refreshed.AccessToken, refreshed.RefreshToken, refreshed.ExpiresIn);

        await SaveAuthInfoAsync(authInfo);

        return authInfo;
    }

    private async Task<string> GetRefreshTokenFromConfigAsync()
    {
        try
        {
            var setting = await configClient.GetConfigurationSettingAsync(RefreshTokenKey);
            return setting.Value.Value;
        }
        catch (Exception ex)
        {
            logger.LogError($"Failed to read refresh token from App Configuration: {ex.Message}");
            return null;
        }
    }

    private async Task WriteRefreshTokenAsync(string refreshToken)
    {
        try
        {
            await configClient.SetConfigurationSettingAsync(RefreshTokenKey, refreshToken);
        }
        catch (Exception ex)
        {
            logger.LogError($"Failed to write refresh token to App Configuration: {ex.Message}");
        }
    }
}
