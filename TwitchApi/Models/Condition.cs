using System.Text.Json.Serialization;

namespace TwitchApi.Models.Responses;

public class Condition
{
    [JsonPropertyName("broadcaster_user_id")]
    public string BroadcasterUserId { get; set; }
}
