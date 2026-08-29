using XeniaBot.WebPanel.Models;
using XeniaDiscord.Data.Models.BanSync;

namespace XeniaBot.WebPanel.Areas.BanSync.Models;

public class BanSyncRecordViewModel
{
    public required BanSyncRecordModel Record { get; set; }
    public AlertComponentViewModel? Alert { get; set; }
}