using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TwitchApi.Models;

public class BroadcasterSubsciptionResponse : TwitchResponse
{
    [JsonPropertyName("data")]
    public IEnumerable<Subscriber> Data { get; set; }
}
