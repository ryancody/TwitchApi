using System.Text.Json.Serialization;

namespace TwitchApi.Models;

public class Subscription
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; }

    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; }
}
