namespace XeniaBot.WebPanel.Models.BanSyncSearch;

public class MutualRecordsListModel
{
    public ulong GuildId { get; set; }
    public string? GuildName { get; set; } = "unknown";
    public string? GuildIconUrl { get; set; }
    public long? MemberCount { get; set; }

    public required MutualRecordsListComponentModel Component { get; set; }

    public long ThisServerRecordCount { get; set; }
    public long OtherServerRecordCount { get; set; }
}
