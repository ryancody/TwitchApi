using System.Text.Json.Serialization;

namespace TwitchApi.Models.Events;

public class CustomPowerUp
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }
    
    [JsonPropertyName("bits")]
    public int? Bits { get; set; }

    [JsonPropertyName("prompt")]
    public string Prompt { get; set; }
}
