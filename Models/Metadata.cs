using System.Text.Json.Serialization;

namespace TwitchApi.Models;

public class Metadata
{
    [JsonPropertyName("message_id")]
    public string MessageId { get; set; }

    [JsonPropertyName("message_type")]
    public string MessageType { get; set; }

    [JsonPropertyName("message_timestamp")]
    public string MessageTimestamp { get; set; }
}
