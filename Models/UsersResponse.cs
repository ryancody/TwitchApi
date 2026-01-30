using System.Text.Json.Serialization;

namespace TwitchApi.Models;

public class UsersResponse : TwitchResponse
{
    [JsonPropertyName("data")]
    public IEnumerable<User> Data { get; set; } = [];
}
