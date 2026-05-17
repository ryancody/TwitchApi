using System.Text.Json.Serialization;

namespace TwitchApi.Models.Events;

/// <summary>
/// The channel.chat.message subscription type sends a notification when any user sends a message to a channel’s chat room.
/// </summary>
public class ChannelChatMessageEvent : Event
{
    public const string ChannelChatMessageEventType = "channel.chat.message";

    [JsonPropertyName("source_broadcaster_user_id")]
    public string SourceBroadcasterUserId { get; set; }

    [JsonPropertyName("source_broadcaster_user_login")]
    public string SourceBroadcasterUserLogin { get; set; }

    [JsonPropertyName("source_broadcaster_user_name")]
    public string SourceBroadcasterUserName { get; set; }

    [JsonPropertyName("chatter_user_id")]
    public string ChatterUserId { get; set; }

    [JsonPropertyName("chatter_user_login")]
    public string ChatterUserLogin { get; set; }

    [JsonPropertyName("chatter_user_name")]
    public string ChatterUserName { get; set; }

    [JsonPropertyName("message_id")]
    public string MessageId { get; set; }

    [JsonPropertyName("source_message_id")]
    public string SourceMessageId { get; set; }

    [JsonPropertyName("is_source_only")]
    public bool? IsSourceOnly { get; set; }

    [JsonPropertyName("message")]
    public Message Message { get; set; }

    [JsonPropertyName("color")]
    public string Color { get; set; }

    [JsonPropertyName("badges")]
    public IEnumerable<Badge> Badges { get; set; }

    [JsonPropertyName("source_badges")]
    public IEnumerable<Badge> SourceBadges { get; set; }

    [JsonPropertyName("message_type")]
    public string MessageType { get; set; }

    [JsonPropertyName("cheer")]
    public object Cheer { get; set; }

    [JsonPropertyName("reply")]
    public object Reply { get; set; }

    [JsonPropertyName("channel_points_custom_reward_id")]
    public object ChannelPointsCustomRewardId { get; set; }

    [JsonPropertyName("channel_points_animation_id")]
    public object ChannelPointsAnimationId { get; set; }
}
