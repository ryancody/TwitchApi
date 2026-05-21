using System.Text.Json.Serialization;
using TwitchApi.Constants;

namespace TwitchApi.Models.Events;

// <summary>
// The channel.subscription.gift subscription type sends a notification when a user 
// gives one or more gifted subscriptions in a channel.
/// </summary>
public class ChannelSubscriptionGiftEvent : Event
{
    public static string TypeStatic => "channel.subscription.gift";
    public override string Type => TypeStatic;
    public static string VersionStatic => "1";
    public override string Version => VersionStatic;
    public static List<string> RequiredScopesStatic => new List<string> { Scopes.ChannelReadSubscriptions };
    public override List<string> RequiredScopes => RequiredScopesStatic;

    [JsonPropertyName("cumulative_months")]
    public int? CumulativeMonths { get; set; }

    [JsonPropertyName("is_anonymous")]
    public bool? IsAnonymous { get; set; }
    
    [JsonPropertyName("tier")]
    public string Tier { get; set; }

    [JsonPropertyName("total")]
    public int? Total { get; set; }

    [JsonPropertyName("user_id")]
    public string UserId { get; set; }

    [JsonPropertyName("user_login")]
    public string UserLogin { get; set; }

    [JsonPropertyName("user_name")]
    public string UserName { get; set; }
}
