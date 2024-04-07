using System.Collections.Generic;
using XeniaBot.Data.Models;

namespace XeniaBot.WebPanel.Models;

public interface IBanSyncViewModel
{
    public ConfigBanSyncModel BanSyncConfig { get; set; }
    public ICollection<BanSyncStateHistoryItemModel> BanSyncStateHistory { get; set; }
}