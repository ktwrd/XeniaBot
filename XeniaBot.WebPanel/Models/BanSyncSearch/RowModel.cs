using XeniaDiscord.Data.Models.BanSync;

namespace XeniaBot.WebPanel.Models.BanSyncSearch;

public class RowModel
{
    public required BanSyncRecordModel Record { get; set; }
    public required ulong GuildId { get; set; }
    public required bool IsLast { get; set; }
    public required int Page { get; set; }
}
