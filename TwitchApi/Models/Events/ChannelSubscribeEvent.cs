using System.Text.Json.Serialization;

namespace TwitchApi.Models.Events;

// <summary>
// The channel.subscription.gift subscription type sends a notification when a user 
// gives one or more gifted subscriptions in a channel.
/// </summary>
public class ChannelSubscribeEvent : Event
{
    public const string SubscribeEventType = "channel.subscribe";

    [JsonPropertyName("is_gift")]
    public bool? IsGift { get; set; }

    [JsonPropertyName("tier")]
    public string Tier { get; set; }

    [JsonPropertyName("user_id")]
    public string UserId { get; set; }

    [JsonPropertyName("user_login")]
    public string UserLogin { get; set; }

    [JsonPropertyName("user_name")]
    public string UserName { get; set; }
}
