using System.Text.Json.Serialization;

namespace TwitchApi.Models.Events;

// <summary>
// The channel.custom_power_up_redemption.add subscription type sends a notification when a user 
// redeems a custom power up.
/// </summary>
public class ChannelCustomPowerUpRedemptionAddEvent : Event
{
    public static string TypeStatic => "channel.custom_power_up_redemption.add";
    public override string Type => TypeStatic;
    public static string VersionStatic => "1";
    public override string Version => VersionStatic;
    public static List<string> RequiredScopesStatic => ["bits:read"];
    public override List<string> RequiredScopes => RequiredScopesStatic;

    [JsonPropertyName("user_id")]
    public string UserId { get; set; }

    [JsonPropertyName("user_login")]
    public string UserLogin { get; set; }

    [JsonPropertyName("user_name")]
    public string UserName { get; set; }

    [JsonPropertyName("user_input")]
    public string UserInput { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("custom_power_up")]
    public CustomPowerUp CustomPowerUp { get; set; }

    [JsonPropertyName("redeemed_at")]
    public DateTime RedeemedAt { get; set; }
}
