namespace TwitchApi.Models.Events;

public interface IEvent
{
    public string Type { get; }
    public string Version { get; }
    public List<string> RequiredScopes { get; }
}
