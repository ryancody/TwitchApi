using System.Text.Json.Serialization;
using TwitchApi.Constants;

namespace TwitchApi.Models.Events;

// <summary>
// The channel.subscribe subscription type sends a notification when a user 
// subscribes to a channel.
/// </summary>
public class ChannelChannelPointsCustomRewardRedemptionAdd : Event
{
    public static string TypeStatic => "channel.channel_points_custom_reward_redemption.add";
    public override string Type => TypeStatic;
    public static string VersionStatic => "1";
    public override string Version => VersionStatic;
    public static List<string> RequiredScopesStatic => new List<string> { Scopes.ChannelReadRedemptions };
    public override List<string> RequiredScopes => RequiredScopesStatic;

    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("redeemed_at")]
    public string RedeemedAt { get; set; }

    [JsonPropertyName("reward")]
    public Reward Reward { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; }
}
