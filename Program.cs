using System.Text.Json;
using TwitchApi;
using TwitchApi.Models;

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

var clientInfo = JsonSerializer.Deserialize<TwitchApi.Models.AppInfo>(clientInfoString);

ArgumentNullException.ThrowIfNull(clientInfo);

client = new TwitchClient(clientInfo, "");
client.MessageReceived += (Event @event) =>
{
    Console.WriteLine($"Received {@event.MessageType}");
};

client.ConnectAsync().GetAwaiter().GetResult();

await Task.Delay(-1);
