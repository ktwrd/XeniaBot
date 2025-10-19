using System.ComponentModel.DataAnnotations;

namespace XeniaDiscord.Data.Models.BanSync;

public interface IBanSyncRecordModel
{
    /// <summary>
    /// Record Id (Guid)
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Discord User Id that was banned (Snowflake)
    /// </summary>
    [MaxLength(DbGlobals.UnsignedLongStringLength)]
    public string UserId { get; set; }

    /// <summary>
    /// Discord Guild Id where that user was banned (Snowflake)
    /// </summary>
    [MaxLength(DbGlobals.UnsignedLongStringLength)]
    public string GuildId { get; set; }

    /// <summary>
    /// Guild Name
    /// </summary>
    [MaxLength(DbGlobals.MaxLength.Discord.GuildName)]
    public string GuildName { get; set; }

    /// <summary>
    /// UTC Time when the user got banned.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Reason why the user was banned (optional)
    /// </summary>
    [MaxLength(DbGlobals.MaxLength.Discord.BanReason)]
    public string? Reason { get; set; }

    /// <summary>
    /// Username of the person that was banned.
    /// </summary>
    [MaxLength(DbGlobals.MaxLength.Discord.Username)]
    public string? Username { get; set; }

    /// <summary>
    /// Display Name of the person that was banned.
    /// </summary>
    [MaxLength(DbGlobals.MaxLength.Discord.DisplayName)]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Discord User Id that banned <see cref="UserId"/> (Snowflake, optional)
    /// </summary>
    [MaxLength(DbGlobals.UnsignedLongStringLength)]
    public string? CreatedByUserId { get; set; }

    /// <summary>
    /// Username of the person that banned <see cref="UserId"/>
    /// </summary>
    [MaxLength(DbGlobals.MaxLength.Discord.Username)]
    public string? CreatedByUsername { get; set; }

    /// <summary>
    /// Display Name of the person that banned <see cref="UserId"/>
    /// </summary>
    [MaxLength(DbGlobals.MaxLength.Discord.DisplayName)]
    public string? CreatedByUserDisplayName { get; set; }
}
