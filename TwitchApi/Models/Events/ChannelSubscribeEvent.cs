using System.Text.Json.Serialization;
using TwitchApi.Constants;

namespace TwitchApi.Models.Events;

// <summary>
// The channel.subscribe subscription type sends a notification when a user 
// subscribes to a channel.
/// </summary>
public class ChannelSubscribeEvent : Event
{
    public static string TypeStatic => "channel.subscribe";
    public override string Type => TypeStatic;
    public static string VersionStatic => "1";
    public override string Version => VersionStatic;
    public static List<string> RequiredScopesStatic => new List<string> { Scopes.ChannelReadSubscriptions };
    public override List<string> RequiredScopes => RequiredScopesStatic;

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
