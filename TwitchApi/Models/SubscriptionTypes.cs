namespace TwitchApi.Models;

public static class SubscriptionTypes
{
    public static readonly SubscriptionType ChannelUpdate = new SubscriptionType { Name = "channel.update", Version = "2" };
    public static readonly SubscriptionType ChannelChatMessage = new SubscriptionType { Name = "channel.chat.message", Version = "1" };
    public static readonly SubscriptionType ChannelSubscribe = new SubscriptionType { Name = "channel.subscribe", Version = "1" };
}
