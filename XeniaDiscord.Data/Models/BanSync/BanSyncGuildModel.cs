using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace XeniaDiscord.Data.Models.BanSync;

public class BanSyncGuildModel : IBanSyncGuildModel
{
    public const string TableName = "BanSyncGuild";

    public BanSyncGuildModel()
    {
        Id = "0";
        LogChannelId = "0";
        CreatedAt = DateTime.UtcNow;
        Enabled = false;
        State = BanSyncGuildState.Unknown;
    }

    /// <inheritdoc/>
    [MaxLength(DbGlobals.UnsignedLongStringLength)]
    public string Id { get; set; }

    /// <inheritdoc/>
    [MaxLength(DbGlobals.MaxLength.Discord.GuildName)]
    public string? GuildName { get; set; }

    /// <inheritdoc/>
    [MaxLength(DbGlobals.UnsignedLongStringLength)]
    public string? LogChannelId { get; set; }

    /// <inheritdoc/>
    [DefaultValue(false)]
    public bool Enabled { get; set; }

    /// <inheritdoc/>
    public BanSyncGuildState State { get; set; }

    /// <inheritdoc/>
    [MaxLength(4000)]
    public string? InternalNote { get; set; }

    /// <inheritdoc/>
    public DateTime CreatedAt { get; set; }

    /// <inheritdoc/>
    public DateTime? UpdatedAt { get; set; }

    /// <inheritdoc/>
    public string? UpdatedByUserId { get; set; }

    public ulong? GetGuildId()
    {
        if (string.IsNullOrEmpty(Id)) return null;
        if (ulong.TryParse(Id, out var result)) return result;
        return null;
    }
    public ulong? GetLogChannelId()
    {
        if (string.IsNullOrEmpty(LogChannelId)) return null;
        if (ulong.TryParse(LogChannelId, out var result)) return result;
        return null;
    }

    public BanSyncGuildSnapshotModel ToSnapshot()
    {
        return new BanSyncGuildSnapshotModel()
        {
            Id = Id,
            GuildName = GuildName,
            LogChannelId = LogChannelId,
            Enabled = Enabled,
            State = State,
            InternalNote = InternalNote,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
            UpdatedByUserId = UpdatedByUserId
        };
    }
    public BanSyncGuildModel Clone()
    {
        if (this.MemberwiseClone() is BanSyncGuildModel r) return r;
        throw new NotImplementedException();
    }
}
