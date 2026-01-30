using System.Text.Json.Serialization;

namespace TwitchApi.Models.Responses;

public class SubscribeData
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; }

    [JsonPropertyName("condition")]
    public Condition Condition { get; set; }

    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; }

    [JsonPropertyName("transport")]
    public Transport Transport { get; set; }

    [JsonPropertyName("cost")]
    public int Cost { get; set; }
}
