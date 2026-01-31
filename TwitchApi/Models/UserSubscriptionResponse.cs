using System.Text.Json.Serialization;

namespace TwitchApi.Models;

public class UserSubsciptionResponse : TwitchResponse
{
    [JsonPropertyName("data")]
    public object Data { get; set; }

    [JsonPropertyName("broadcaster_id")]
    public string BroadcasterId { get; set; }

    [JsonPropertyName("broadcaster_name")]
    public string BroadcasterName { get; set; }

    [JsonPropertyName("gifter_id")]
    public string GifterId { get; set; }

    [JsonPropertyName("gifter_login")]
    public string GifterLogin { get; set; }

    [JsonPropertyName("gifter_name")]
    public string GifterName { get; set; }

    [JsonPropertyName("tier")]
    public string Tier { get; set; }
}
