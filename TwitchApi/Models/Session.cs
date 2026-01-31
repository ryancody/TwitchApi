using System.Text.Json.Serialization;

namespace TwitchApi.Models;

public class Session
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("connected_at")]
    public string ConnectedAt { get; set; }

    [JsonPropertyName("keepalive_timeout_seconds")]
    public int KeepaliveTimeoutSeconds { get; set; }

    [JsonPropertyName("reconnect_url")]
    public string ReconnectUrl { get; set; }

    [JsonPropertyName("recovery_url")]
    public string RecoveryUrl { get; set; }
}
