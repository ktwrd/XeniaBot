using System.Collections.Generic;
using XeniaDiscord.Data.Models.BanSync;

namespace XeniaBot.WebPanel.Models;

public interface IBanSyncViewModel
{
    public BanSyncGuildModel BanSyncConfig { get; set; }
    public ICollection<BanSyncGuildSnapshotModel> BanSyncStateHistory { get; set; }
}