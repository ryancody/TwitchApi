using System.Text.Json.Serialization;

namespace TwitchApi.Models;

public class WebSocketMessage
{
    [JsonPropertyName("metadata")]
    public Metadata Metadata { get; set; }

    [JsonPropertyName("payload")]
    public Payload Payload { get; set; }
}
