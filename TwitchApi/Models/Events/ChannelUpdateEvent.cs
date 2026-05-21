using System.Text.Json.Serialization;

namespace TwitchApi.Models.Events;

// <summary>
// The channel.subscribe subscription type sends a notification when a user 
// subscribes to a channel.
/// </summary>
public class ChannelUpdateEvent : Event
{
    public static string TypeStatic => "channel.update";
    public override string Type => TypeStatic;
    public static string VersionStatic => "1";
    public override string Version => VersionStatic;
    public static List<string> RequiredScopesStatic => [];
    public override List<string> RequiredScopes => RequiredScopesStatic;

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("language")]
    public string Language { get; set; }

    [JsonPropertyName("category_id")]
    public string CategoryId { get; set; }

    [JsonPropertyName("category_name")]
    public string CategoryName { get; set; }

    [JsonPropertyName("content_classification_labels")]
    public IEnumerable<string> ContentClassificationLabels { get; set; }
}
