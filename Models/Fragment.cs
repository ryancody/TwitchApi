using System.Text.Json.Serialization;

namespace TwitchApi.Models;

public class Fragment
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; }

    [JsonPropertyName("cheermote")]
    public object Cheermote { get; set; }

    [JsonPropertyName("emote")]
    public object Emote { get; set; }

    [JsonPropertyName("mention")]
    public object Mention { get; set; }
}
