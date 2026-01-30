using System.Text.Json.Serialization;

namespace TwitchApi.Models;

public class Badge
{
    [JsonPropertyName("set_id")]
    public string SetId { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("info")]
    public string Info { get; set; }
}
