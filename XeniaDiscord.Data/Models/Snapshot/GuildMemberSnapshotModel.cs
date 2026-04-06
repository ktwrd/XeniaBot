using System.ComponentModel.DataAnnotations;

namespace XeniaDiscord.Data.Models.Snapshot;

public class GuildMemberSnapshotModel : IGuildMemberSnapshot
{
    public const string TableName = "Snapshot_GuildMember";
    public GuildMemberSnapshotModel()
    {
        RecordId = Guid.NewGuid();
        RecordCreatedAt = DateTime.UtcNow;
        UserId = "0";
        GuildId = "0";
        Username = "";
    }

    /// <inheritdoc/>
    public Guid RecordId { get; set; }
    /// <inheritdoc/>
    public DateTime RecordCreatedAt { get; set; }

    /// <inheritdoc/>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string UserId { get; set; }
    /// <inheritdoc/>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string GuildId { get; set; }

    /// <inheritdoc/>
    [MaxLength(DbGlobals.MaxLength.DiscordUsername)]
    public string Username { get; set; }
    /// <inheritdoc/>
    [MaxLength(DbGlobals.MaxLength.DiscordDiscriminator)]
    public string? Discriminator { get; set; }
    /// <inheritdoc/>
    public string? Nickname { get; set; }
    /// <inheritdoc/>
    public bool IsSelfDeafened { get; set; }
    /// <inheritdoc/>
    public bool IsSelfMuted { get; set; }
    /// <inheritdoc/>
    public bool IsSuppressed { get; set; }
    /// <inheritdoc/>
    public bool IsDeafened { get; set; }
    /// <inheritdoc/>
    public bool IsMuted { get; set; }
    /// <inheritdoc/>
    public bool IsStreaming { get; set; }
    /// <inheritdoc/>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string? VoiceChannelId { get; set; }
    /// <inheritdoc/>
    public string? GuildAvatarId { get; set; }
    /// <inheritdoc/>
    public DateTime? JoinedAt { get; set; }
    /// <inheritdoc/>
    public DateTime? TimedOutUntil { get; set; }
    /// <inheritdoc/>
    public Discord.GuildUserFlags Flags { get; set; }
    /// <inheritdoc/>
    public Discord.UserProperties? PublicFlags { get; set; }
    /// <inheritdoc/>
    public bool? IsPending { get; set; }
    public string? AvatarUrl { get; set; }

    /// <inheritdoc/>
    public ulong GetUserId() => UserId.ParseRequiredULong(nameof(UserId), false);
    /// <inheritdoc/>
    public ulong GetGuildId() => GuildId.ParseRequiredULong(nameof(GuildId), false);
    /// <inheritdoc/>
    public ulong? GetVoiceChannelId() => VoiceChannelId.ParseULong(false);

    public List<GuildMemberRoleSnapshotModel> Roles { get; set; } = [];
    public List<GuildMemberPermissionSnapshotModel> Permissions { get; set; } = [];

    /// <summary>
    /// Check if the roles in the <paramref name="other"/> model are exactly the same as our <see cref="Roles"/>.
    /// Will always return <see langword="true"/> when <paramref name="other"/> is <see langword="null"/>
    /// </summary>
    public bool RolesMatch(GuildMemberSnapshotModel? other)
    {
        if (other == null) return false;
        var ourRoleIds = Roles.Select(e => e.GetRoleId()).ToHashSet();
        var otherRoles = other.Roles.Select(e => e.GetRoleId()).ToHashSet();
        var allRoles = ourRoleIds.Concat(otherRoles).ToHashSet();
        if (allRoles.Count != Roles.Count) return true;
        return allRoles.Any(id => !ourRoleIds.Contains(id) || !otherRoles.Contains(id));
    }
}

public interface IGuildMemberSnapshot : ISnapshot
{
    /// <summary>
    /// User Id (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string UserId { get; set; }

    /// <summary>
    /// Guild Id (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string GuildId { get; set; }

    /// <remarks>
    /// Value from: <see cref="Discord.IUser.Username"/>
    /// </remarks>
    [MaxLength(DbGlobals.MaxLength.DiscordUsername)]
    public string Username { get; set; }
    /// <remarks>
    /// Value from: <see cref="Discord.IUser.Discriminator"/>
    /// </remarks>
    [MaxLength(DbGlobals.MaxLength.DiscordDiscriminator)]
    public string? Discriminator { get; set; }
    /// <remarks>
    /// Value from: <see cref="Discord.IGuildUser.Nickname"/>
    /// </remarks>
    public string? Nickname { get; set; }
    /// <remarks>
    /// Value from: <see cref="Discord.IVoiceState.IsSelfDeafened"/>
    /// </remarks>
    public bool IsSelfDeafened { get; set; }
    /// <remarks>
    /// Value from: <see cref="Discord.IVoiceState.IsSelfMuted"/>
    /// </remarks>
    public bool IsSelfMuted { get; set; }
    /// <remarks>
    /// Value from: <see cref="Discord.IVoiceState.IsSelfSuppressed"/>
    /// </remarks>
    public bool IsSuppressed { get; set; }
    /// <remarks>
    /// Value from: <see cref="Discord.IVoiceState.IsDeafened"/>
    /// </remarks>
    public bool IsDeafened { get; set; }
    /// <remarks>
    /// Value from: <see cref="Discord.IVoiceState.IsMuted"/>
    /// </remarks>
    public bool IsMuted { get; set; }
    /// <remarks>
    /// Value from: <see cref="Discord.IVoiceState.IsStreaming"/>
    /// </remarks>
    public bool IsStreaming { get; set; }
    /// <remarks>
    /// Value from: <see cref="Discord.IVoiceChannel.Id"/> (from property <see cref="Discord.IVoiceState.VoiceChannel"/>)
    /// </remarks>
    public string? VoiceChannelId { get; set; }
    /// <remarks>
    /// Value from: <see cref="Discord.IGuildUser.GuildAvatarId"/>
    /// </remarks>
    public string? GuildAvatarId { get; set; }
    /// <summary>
    /// UTC Time when this user joined.
    /// </summary>
    /// <remarks>
    /// Value from: <see cref="Discord.IGuildUser.JoinedAt"/>
    /// </remarks>
    public DateTime? JoinedAt { get; set; }
    /// <summary>
    /// UTC Time when the user is timed out until
    /// </summary>
    /// <remarks>
    /// Value from: <see cref="Discord.IGuildUser.TimedOutUntil"/>
    /// </remarks>
    public DateTime? TimedOutUntil { get; set; }
    /// <remarks>
    /// Value from: <see cref="Discord.IGuildUser.Flags"/>
    /// </remarks>
    public Discord.GuildUserFlags Flags { get; set; }
    /// <remarks>
    /// Value from: <see cref="Discord.IUser.PublicFlags"/>
    /// </remarks>
    public Discord.UserProperties? PublicFlags { get; set; }
    /// <remarks>
    /// Value from: <see cref="Discord.IGuildUser.IsPending"/>
    /// </remarks>
    public bool? IsPending { get; set; }

    public ulong GetUserId();
    public ulong GetGuildId();
    public ulong? GetVoiceChannelId();
}

