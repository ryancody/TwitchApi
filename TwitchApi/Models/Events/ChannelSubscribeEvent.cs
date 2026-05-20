using System.Text.Json.Serialization;
using TwitchApi.Constants;

namespace TwitchApi.Models.Events;

// <summary>
// The channel.subscribe subscription type sends a notification when a user 
// subscribes to a channel.
/// </summary>
public class ChannelSubscribeEvent : Event
{
    public override string Type => "channel.subscribe";
    public override string Version => "1";
    public override List<string> RequiredScopes => new List<string> { Scopes.ChannelReadSubscriptions };

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
