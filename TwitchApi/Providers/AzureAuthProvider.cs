using Azure.Data.AppConfiguration;
using Microsoft.Extensions.Logging;
using TwitchApi.Models;
using TwitchApi.Providers.Constants;
using TwitchApi.Providers.Models;

namespace TwitchApi.Providers;

public class AzureAuthProvider : IAuthProvider
{
    private readonly TwitchHttpClient httpClient;
    private readonly ILogger logger;
    private readonly ConfigurationClient configClient;
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

        var authInfo = await GetAuthInfoFromConfigAsync();
        await SaveAuthInfoAsync(authInfo);

        if (string.IsNullOrWhiteSpace(authInfo?.RefreshToken))
        {
            logger.LogError("No refresh token found in App Configuration");
            return null;
        }

        return await RefreshAsync(authInfo?.RefreshToken);
    }

    public async Task SaveAuthInfoAsync(AuthInfo authInfo)
    {
        cachedAuthInfo = authInfo;
        await WriteRefreshTokenAsync(authInfo.RefreshToken);
    }

    private async Task<AuthInfo> RefreshAsync(string refreshToken)
    {
        logger.LogInformation("Refreshing auth token");

        var refreshed = await httpClient.RefreshTokenAsync(cachedAuthInfo.ClientId, cachedAuthInfo.ClientSecret, refreshToken);

        if (!refreshed.IsSuccessStatusCode)
        {
            logger.LogError("Failed to refresh auth token");
            return null;
        }

        var authInfo = new AuthInfo(
            cachedAuthInfo.ClientId,
            cachedAuthInfo.ClientSecret,
            refreshed.AccessToken,
            refreshed.RefreshToken,
            refreshed.ExpiresIn);

        await SaveAuthInfoAsync(authInfo);

        return authInfo;
    }

    private async Task<AuthInfo> GetAuthInfoFromConfigAsync()
    {
        try
        {
            var refreshTokenSettingTask = configClient.GetConfigurationSettingAsync(AppConfigConstants.RefreshTokenKey);
            var clientIdSettingTask = configClient.GetConfigurationSettingAsync(AppConfigConstants.ClientIdTokenKey);
            var clientSecretSettingTask = configClient.GetConfigurationSettingAsync(AppConfigConstants.ClientSecretTokenKey);

            await Task.WhenAll(refreshTokenSettingTask, clientIdSettingTask, clientSecretSettingTask);

            ArgumentException.ThrowIfNullOrWhiteSpace(refreshTokenSettingTask.Result?.Value.Value, "Refresh token not found in App Configuration");
            ArgumentException.ThrowIfNullOrWhiteSpace(clientIdSettingTask.Result?.Value.Value, "Client ID not found in App Configuration");
            ArgumentException.ThrowIfNullOrWhiteSpace(clientSecretSettingTask.Result?.Value.Value, "Client secret not found in App Configuration");

            return new AuthInfo(
                 clientIdSettingTask.Result?.Value.Value,
                 clientSecretSettingTask.Result?.Value.Value,
                 null,
                 refreshTokenSettingTask.Result?.Value.Value,
                 0);
        }
        catch (Exception ex)
        {
            logger.LogError($"Failed to read refresh token from App Configuration: {ex.Message}");
            return null;
        }
    }

    private async Task WriteRefreshTokenAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return;

        try
        {
            await configClient.SetConfigurationSettingAsync(AppConfigConstants.RefreshTokenKey, refreshToken);
        }
        catch (Exception ex)
        {
            logger.LogError($"Failed to write refresh token to App Configuration: {ex.Message}");
        }
    }
}
