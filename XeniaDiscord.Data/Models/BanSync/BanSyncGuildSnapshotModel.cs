using System.ComponentModel.DataAnnotations;

namespace XeniaDiscord.Data.Models.BanSync;

public class BanSyncGuildSnapshotModel
{
    public const string TableName = "BanSyncGuildSnapshots";

    public BanSyncGuildSnapshotModel()
    {
        Id = Guid.NewGuid();
        Timestamp = DateTime.UtcNow;
        GuildId = "0";
    }
    public BanSyncGuildSnapshotModel(BanSyncGuildModel model) : this()
    {
        GuildId = model.GuildId;
        LogChannelId = model.LogChannelId;
        Enable = model.Enable;
        State = model.State;
        Notes = model.Notes;
    }

    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Guild Id (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string GuildId { get; set; }

    /// <summary>
    /// Channel Id (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string? LogChannelId { get; set; }

    public bool Enable { get; set; }
    public BanSyncGuildState State { get; set; }
    public string? Notes { get; set; }

    public string? UpdatedByUserId { get; set; }

    public ulong GetGuildId() => GuildId.ParseRequiredULong(nameof(GuildId), false);
    public ulong? GetUpdatedByUserId() => UpdatedByUserId?.ParseULong(false);
}
