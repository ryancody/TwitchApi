using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TwitchApi.Providers;
using TwitchApi.Providers.Models;

namespace TwitchApi.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTwitchApi(this IServiceCollection services, string channelName, string appId)
    {
        services.AddSingleton(sp => new TwitchHttpClient(appId));

        var isAzure = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID"));

        if (isAzure)
        {
            services.AddLogging();
            services.AddSingleton<IAuthProvider, AzureAuthProvider>(sp =>
            {
                var twitchHttpClient = sp.GetRequiredService<TwitchHttpClient>();
                var logger = sp.GetRequiredService<ILogger<AzureAuthProvider>>();
                return new AzureAuthProvider(twitchHttpClient, logger);
            });
        }
        else
        {
            services.AddSingleton<IAuthProvider, LocalAuthProvider>(sp =>
            {
                var twitchHttpClient = sp.GetRequiredService<TwitchHttpClient>();
                var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
                var logger = loggerFactory.CreateLogger<LocalAuthProvider>();
                return new LocalAuthProvider(appId, twitchHttpClient, logger);
            });
        }

        services.AddSingleton(sp =>
        {
            var authProvider = sp.GetRequiredService<IAuthProvider>();
            var twitchHttpClient = sp.GetRequiredService<TwitchHttpClient>();
            var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
            var logger = loggerFactory.CreateLogger<TwitchClient>();
            return new TwitchClient(channelName, twitchHttpClient, authProvider, logger);
        });
        return services;
    }
}
