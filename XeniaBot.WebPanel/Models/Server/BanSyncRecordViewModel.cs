using XeniaDiscord.Data.Models.BanSync;

namespace XeniaBot.WebPanel.Models;

public class BanSyncRecordViewModel : BaseViewModel
{
    public required BanSyncRecordModel Record { get; set; }
}