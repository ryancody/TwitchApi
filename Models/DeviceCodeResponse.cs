using System.Text.Json.Serialization;

namespace TwitchApi.Models;

public class DeviceCodeResponse : TwitchResponse
{
    [JsonPropertyName("device_code")]
    public string DeviceCode { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("interval")]
    public int Interval { get; set; }

    [JsonPropertyName("user_code")]
    public string UserCode { get; set; }

    [JsonPropertyName("verification_uri")]
    public string VerificationUri { get; set; }
}
