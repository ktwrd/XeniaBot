using MongoDB.Bson;
using System;
using System.ComponentModel;

namespace XeniaBot.MongoData.Models;

[Obsolete("Use XeniaDiscord.Data.Models.BanSync.BanSyncGuildSnapshotModel")]
public class ConfigBanSyncModel
{
    [Browsable(false)]
    public ObjectId _id { get; set; }
    public ulong GuildId { get; set; }
    public ulong LogChannel { get; set; }
    public bool Enable { get; set; }
    public BanSyncGuildState State { get; set; }
    public string Reason { get; set; }
    public ConfigBanSyncModel()
    {
        GuildId = 0;
        LogChannel = 0;
        Enable = false;
        State = BanSyncGuildState.Unknown;
        Reason = "";
    }
}

[Obsolete("Use XeniaDiscord.Data.Models.BanSync.BanSyncGuildState")]
public enum BanSyncGuildState
{
    Unknown = -1,
    PendingRequest,
    RequestDenied,
    Blacklisted,
    Active
}
