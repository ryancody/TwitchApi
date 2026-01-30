using System.Text.Json.Serialization;

namespace TwitchApi.Models.Responses;

public class Transport
{
    [JsonPropertyName("method")]
    public string Method { get; set; }

    [JsonPropertyName("session_id")]
    public string SessionId { get; set; }

    [JsonPropertyName("connected_at")]
    public string ConnectedAt { get; set; }
}
