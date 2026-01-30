using System.Text.Json.Serialization;

namespace TwitchApi.Models;

public class AppInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
}
