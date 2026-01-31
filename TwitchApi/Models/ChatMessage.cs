using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TwitchApi.Models;

public class ChatMessage
{
    [JsonPropertyName("text")]
    public string Text { get; set; }

    [JsonPropertyName("fragments")]
    public IEnumerable<Fragment> Fragments { get; set; }
}
