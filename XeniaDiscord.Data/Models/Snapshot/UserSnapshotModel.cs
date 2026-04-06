using System.ComponentModel.DataAnnotations;

namespace XeniaDiscord.Data.Models.Snapshot;

public class UserSnapshotModel : IUserSnapshot
{
    public const string TableName = "Snapshot_User";
    public UserSnapshotModel()
    {
        RecordId = Guid.NewGuid();
        RecordCreatedAt = DateTime.UtcNow;
        UserId = "0";
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
    [MaxLength(DbGlobals.MaxLength.DiscordUsername)]
    public string Username { get; set; }
    /// <inheritdoc/>
    [MaxLength(DbGlobals.MaxLength.DiscordDiscriminator)]
    public string? Discriminator { get; set; }
    /// <inheritdoc/>
    public string? GlobalName { get; set; }
    /// <inheritdoc/>
    public bool IsBot { get; set; }
    /// <inheritdoc/>
    public bool IsWebhook { get; set; }
    /// <inheritdoc/>
    public Discord.UserProperties? PublicFlags { get; set; }
    /// <inheritdoc/>
    public string? AvatarId { get; set; }
    /// <inheritdoc/>
    public string? AvatarDecorationSkuId { get; set; }
    /// <inheritdoc/>
    public string? AvatarDecorationHash { get; set; }
    /// <inheritdoc/>
    public Guid? PrimaryGuildId { get; set; }
    /// <inheritdoc/>
    public string? AvatarUrl { get; set; }
    /// <inheritdoc/>
    public string? DefaultAvatarUrl { get; set; }
    /// <inheritdoc/>
    public string? DisplayAvatarUrl { get; set; }
    /// <inheritdoc/>
    public string? AvatarDecorationUrl { get; set; }
    /// <inheritdoc/>
    public Discord.UserStatus Status { get; set; }

    /// <inheritdoc/>
    public ulong GetUserId() => UserId.ParseRequiredULong(nameof(UserId), false);

    /// <summary>
    /// Property Accessor
    /// </summary>
    public PrimaryGuildSnapshotModel? PrimaryGuild { get; set; }
}

public interface IUserSnapshot : ISnapshot
{
    /// <summary>
    /// User Id (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string UserId { get; set; }

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
    /// Value from: <see cref="Discord.IUser.GlobalName"/>
    /// </remarks>
    public string? GlobalName { get; set; }
    /// <remarks>
    /// Value from: <see cref="Discord.IUser.IsBot"/>
    /// </remarks>
    public bool IsBot { get; set; }
    /// <remarks>
    /// Value from: <see cref="Discord.IUser.IsWebhook"/>
    /// </remarks>
    public bool IsWebhook { get; set; }
    /// <remarks>
    /// Value from: <see cref="Discord.IUser.PublicFlags"/>
    /// </remarks>
    public Discord.UserProperties? PublicFlags { get; set; }
    /// <remarks>
    /// Value from: <see cref="Discord.IUser.AvatarId"/>
    /// </remarks>
    public string? AvatarId { get; set; }

    /// <remarks>
    /// Value from: <see cref="Discord.IUser.AvatarDecorationSkuId"/>
    /// </remarks>
    public string? AvatarDecorationSkuId { get; set; }
    /// <remarks>
    /// Value from: <see cref="Discord.IUser.AvatarDecorationHash"/>
    /// </remarks>
    public string? AvatarDecorationHash { get; set; }
    
    /// <summary>
    /// Foreign Key to <see cref="IPrimaryGuildSnapshot.RecordId"/>
    /// </summary>
    public Guid? PrimaryGuildId { get; set; }
    
    /// <remarks>
    /// Value from: <see cref="Discord.IUser.GetAvatarUrl(Discord.ImageFormat, ushort)"/>
    /// </remarks>
    public string? AvatarUrl { get; set; }
    /// <remarks>
    /// Value from: <see cref="Discord.IUser.GetDefaultAvatarUrl"/>
    /// </remarks>
    public string? DefaultAvatarUrl { get; set; }
    /// <remarks>
    /// Value from: <see cref="Discord.IUser.GetDisplayAvatarUrl(Discord.ImageFormat, ushort)"/>
    /// </remarks>
    public string? DisplayAvatarUrl { get; set; }
    /// <remarks>
    /// Value from: <see cref="Discord.IUser.GetAvatarDecorationUrl"/>
    /// </remarks>
    public string? AvatarDecorationUrl { get; set; }

    /// <remarks>
    /// Value from: <see cref="Discord.IPresence.Status"/>
    /// </remarks>
    public Discord.UserStatus Status { get; set; }

    public ulong GetUserId();
}