using System.Text.Json.Serialization;

namespace TwitchApi.Models.Events;

// <summary>
// The channel.subscribe subscription type sends a notification when a user 
// subscribes to a channel.
/// </summary>
public class ChannelUpdateEvent : Event
{
    public override string Type => "channel.update";
    public override string Version => "1";
    public override List<string> RequiredScopes => [];

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
