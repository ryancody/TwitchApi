using System.Text.Json;
using TwitchApi;
using TwitchApi.Models;

if (!Console.IsOutputRedirected)
    Console.Clear();

Console.WriteLine("Running...");

TwitchClient client;

var clientInfoString = File.ReadAllText("../twitch_client_info.json")?.Trim();

if (string.IsNullOrWhiteSpace(clientInfoString))
{
    Console.WriteLine("Twitch client info not found");
    return;
}

var clientInfo = JsonSerializer.Deserialize<AppInfo>(clientInfoString);

ArgumentNullException.ThrowIfNull(clientInfo);

client = new TwitchClient(clientInfo, new Logger());
client.MessageReceived += (Event @event) =>
{
    Console.WriteLine($"Received {@event.MessageType}");
};

await client.ConnectAsync();

await Task.Delay(-1);
