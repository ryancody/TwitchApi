using System.Text.Json.Serialization;
using TwitchApi.Constants;

namespace TwitchApi.Models.Events;

// <summary>
// The channel.subscribe subscription type sends a notification when a user 
// subscribes to a channel.
/// </summary>
public class ChannelChannelPointsCustomRewardRedemptionAdd : Event
{
    public override string Type => "channel.channel_points_custom_reward_redemption.add";
    public override string Version => "1";
    public override List<string> RequiredScopes => new List<string> { Scopes.ChannelReadRedemptions };

    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("redeemed_at")]
    public string RedeemedAt { get; set; }

    [JsonPropertyName("reward")]
    public Reward Reward { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; }
}
