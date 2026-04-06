using System.ComponentModel.DataAnnotations;

namespace XeniaDiscord.Data.Models.Snapshot;

public class PrimaryGuildSnapshotModel : IPrimaryGuildSnapshot
{
    public const string TableName = "Snapshot_PrimaryGuild";
    public PrimaryGuildSnapshotModel()
    {
        RecordId = Guid.NewGuid();
        RecordCreatedAt = DateTime.UtcNow;
        Tag = "";
        BadgeHash = "";
    }
    
    /// <inheritdoc/>
    public Guid RecordId { get; set; }
    /// <inheritdoc/>
    public DateTime RecordCreatedAt { get; set; }

    /// <inheritdoc/>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string? GuildId { get; set; }
    /// <inheritdoc/>
    public bool? IdentityEnabled { get; set; }
    /// <inheritdoc/>
    public string Tag { get; set; }
    /// <inheritdoc/>
    public string BadgeHash { get; set; }
    /// <inheritdoc/>
    public string? BadgeUrl { get; set; }

    /// <inheritdoc/>
    public ulong? GetGuildId() => GuildId?.ParseULong(false);
}
public interface IPrimaryGuildSnapshot : ISnapshot
{
    /// <remarks>
    /// Value from: <see cref="Discord.PrimaryGuild.GuildId"/> (as string)
    /// </remarks>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string? GuildId { get; set; }
    /// <remarks>
    /// Value from: <see cref="Discord.PrimaryGuild.IdentityEnabled"/>
    /// </remarks>
    public bool? IdentityEnabled { get; set; }
    /// <remarks>
    /// Value from: <see cref="Discord.PrimaryGuild.Tag"/>
    /// </remarks>
    public string Tag { get; set; }
    /// <remarks>
    /// Value from: <see cref="Discord.PrimaryGuild.BadgeHash"/>
    /// </remarks>
    public string BadgeHash { get; set; }
    /// <remarks>
    /// Value from: <see cref="Discord.PrimaryGuild.GetBadgeUrl"/>
    /// </remarks>
    public string? BadgeUrl { get; set; }

    public ulong? GetGuildId();
}