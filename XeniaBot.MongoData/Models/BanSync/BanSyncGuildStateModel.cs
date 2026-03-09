using System;
using XeniaBot.Shared.Models;

namespace XeniaBot.MongoData.Models;

[Obsolete("Use XeniaDiscord.Data.Models.BanSync.BanSyncGuildSnapshotModel")]
public class BanSyncStateHistoryItemModel : BaseModel
{
    public const string CollectionName = "banSyncStateHistory";
    public ulong GuildId { get; set; }
    public long Timestamp { get; set; }
    
    public bool Enable { get; set; }
    public BanSyncGuildState State { get; set; }
    public string Reason { get; set; } = "";
}