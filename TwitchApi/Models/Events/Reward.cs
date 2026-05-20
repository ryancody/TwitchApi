using System.Text.Json.Serialization;

namespace TwitchApi.Models.Events;

public class Reward
{
    [JsonPropertyName("cost")]
    public int? Cost { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("prompt")]
    public string Prompt { get; set; }
    
    [JsonPropertyName("title")]
    public string Title { get; set; }
}