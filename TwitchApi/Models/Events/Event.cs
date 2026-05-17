using System.Text.Json.Serialization;

namespace TwitchApi.Models.Events;

public abstract class Event
{
    public abstract string Type { get; }

    [JsonPropertyName("broadcaster_user_id")]
    public string BroadcasterUserId { get; set; }

    [JsonPropertyName("broadcaster_user_login")]
    public string BroadcasterUserLogin { get; set; }

    [JsonPropertyName("broadcaster_user_name")]
    public string BroadcasterUserName { get; set; }
}
