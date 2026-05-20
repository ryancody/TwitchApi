using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TwitchApi;
using TwitchApi.Extensions;
using TwitchApi.Models.Events;

if (!Console.IsOutputRedirected)
    Console.Clear();

Console.WriteLine("Running...");

TwitchClient client;

var clientInfoString = File.ReadAllText("twitch_client_info.json")?.Trim();

if (string.IsNullOrWhiteSpace(clientInfoString))
{
    Console.WriteLine("Twitch client info not found");
    return;
}

var appInfo = JsonDocument.Parse(clientInfoString);

var channel = appInfo.RootElement.GetProperty("channel").GetString();
var id = appInfo.RootElement.GetProperty("id").GetString();

ArgumentNullException.ThrowIfNull(channel);
ArgumentNullException.ThrowIfNull(id);

var subscribedEvents = new Type[]
{
    typeof(ChannelChannelPointsCustomRewardRedemptionAdd),
    typeof(ChannelChatMessageEvent),
    typeof(ChannelSubscribeEvent),
    typeof(ChannelSubscriptionGiftEvent),
    typeof(ChannelSubscriptionMessageEvent),
    typeof(ChannelUpdateEvent)
};
var services = new ServiceCollection()
    .AddTwitchApi(channel, id, subscribedEvents: subscribedEvents, testServer: false)
    .BuildServiceProvider();

client = services.GetRequiredService<TwitchClient>();
client.DeviceAuthorizationRequested += (verificationUri) =>
{
    Process.Start(new ProcessStartInfo(verificationUri) { UseShellExecute = true });
};

client.Connect();

await Task.Delay(-1);
