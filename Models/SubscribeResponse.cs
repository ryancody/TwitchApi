namespace TwitchApi.Models.Responses;

public class SubscribeResponse : TwitchResponse
{
    public IEnumerable<SubscribeData> Data { get; set; }
    public int Total { get; set; }
    public int MaxTotalCost { get; set; }
    public int TotalCost { get; set; }
}
