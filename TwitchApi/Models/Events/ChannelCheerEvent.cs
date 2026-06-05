using System.Text.Json.Serialization;

namespace TwitchApi.Models.Events;

// <summary>
// The channel.cheer subscription type sends a notification when a user 
// cheers in a channel.
/// </summary>
public class ChannelCheerEvent : Event
{
    public static string TypeStatic => "channel.cheer";
    public override string Type => TypeStatic;
    public static string VersionStatic => "1";
    public override string Version => VersionStatic;
    public static List<string> RequiredScopesStatic => ["bits:read"];
    public override List<string> RequiredScopes => RequiredScopesStatic;

    [JsonPropertyName("message")]
    public string Message { get; set; }

    [JsonPropertyName("bits")]
    public int? Bits { get; set; }

    [JsonPropertyName("user_id")]
    public string UserId { get; set; }

    [JsonPropertyName("user_login")]
    public string UserLogin { get; set; }

    [JsonPropertyName("user_name")]
    public string UserName { get; set; }

    [JsonPropertyName("is_anonymous")]
    public bool IsAnonymous { get; set; }
}
