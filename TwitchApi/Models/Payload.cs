using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace TwitchApi.Models;

public class Payload
{
    [JsonPropertyName("session")]
    public Session Session { get; set; }

    [JsonPropertyName("event")]
    public JsonObject Event { get; set; }
    
    [JsonPropertyName("subscription")]
    public Subscription Subscription { get; set; }
}
