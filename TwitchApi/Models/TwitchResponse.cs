using System.Text.Json.Serialization;

namespace TwitchApi.Models;

public class TwitchResponse
{
    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; }

    public bool IsSuccessStatusCode { get; set; }
}
