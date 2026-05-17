using System.Text.Json.Serialization;

namespace TwitchApi.Models.Events;

// <summary>
// The channel.subscription.message subscription type sends a notification when a user sends a resubscription chat message in a specific channel.
/// </summary>
public class ChannelSubscriptionMessageEvent : Event
{
    public const string ChannelSubscriptionMessageEventType = "channel.subscription.message";

    [JsonPropertyName("cumulative_months")]
    public int? CumulativeMonths { get; set; }

    [JsonPropertyName("duration_months")]
    public int? DurationMonths { get; set; }

    [JsonPropertyName("message")]
    public Message Message { get; set; }

    [JsonPropertyName("streak_months")]
    public int? StreakMonths { get; set; }
    
    [JsonPropertyName("tier")]
    public string Tier { get; set; }

    [JsonPropertyName("user_id")]
    public string UserId { get; set; }

    [JsonPropertyName("user_login")]
    public string UserLogin { get; set; }

    [JsonPropertyName("user_name")]
    public string UserName { get; set; }
}
