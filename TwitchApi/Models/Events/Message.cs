using System.Text.Json.Serialization;

namespace TwitchApi.Models.Events;

public class Message
{
    [JsonPropertyName("text")]
    public string Text { get; set; }

    [JsonPropertyName("fragments")]
    public IEnumerable<Fragment> Fragments { get; set; }
}
