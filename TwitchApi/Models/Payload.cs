using System.Text.Json.Serialization;
using TwitchApi.Models.Events;

namespace TwitchApi.Models;

public class Payload
{
    [JsonPropertyName("session")]
    public Session Session { get; set; }

    [JsonPropertyName("event")]
    public Event Event { get; set; }
}
