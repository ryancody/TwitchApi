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
    
var appInfo = JsonDocument.Parse(clientInfoString);

var channel = appInfo.RootElement.GetProperty("channel").GetString();
var id = appInfo.RootElement.GetProperty("id").GetString();

ArgumentNullException.ThrowIfNull(channel);
ArgumentNullException.ThrowIfNull(id);

client = new TwitchClient(channel, id, new Logger());
client.MessageReceived += (Event @event) =>
{
    Console.WriteLine($"Received {@event.MessageType}");
};

await client.ConnectAsync();

await Task.Delay(-1);
