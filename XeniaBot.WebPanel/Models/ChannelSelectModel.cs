namespace XeniaBot.WebPanel.Models;

public class ChannelSelectModel
{
    public IEnumerable<StrippedChannel> Channels { get; set; }
    public ulong? SelectedChannelId { get; set; }
    public string ParentFormId { get; set; }
    public string Name { get; set; }
    public string Id { get; set; }
    public string DisplayName { get; set; }
    public IEnumerable<StrippedCategory> Categories { get; set; }
}