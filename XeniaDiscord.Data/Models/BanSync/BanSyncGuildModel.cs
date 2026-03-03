using System.ComponentModel.DataAnnotations;

namespace XeniaDiscord.Data.Models.BanSync;

public class BanSyncGuildModel
{
    public const string TableName = "BanSyncGuilds";

    public BanSyncGuildModel()
    {
        GuildId = "0";
    }

    /// <summary>
    /// Primary Key: Guild Id (ulong as string)
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

    public ulong GetGuildId() => GuildId.ParseRequiredULong(nameof(GuildId), false);
    public ulong? GetLogChannelId() => LogChannelId?.ParseULong(false);
}
public enum BanSyncGuildState
{
    Unknown = -1,
    PendingRequest,
    RequestDenied,
    Blacklisted,
    Active
}
