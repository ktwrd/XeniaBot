using System.ComponentModel.DataAnnotations;

namespace XeniaDiscord.Data.Models.BanSync;

/// <summary>
/// Model for storing the information about an instance of a user being banned.
/// </summary>
public class BanSyncRecordModel : IBanSyncRecordModel
{
    public const string TableName = "BanSyncRecord";

    public BanSyncRecordModel()
    {
        Id = Guid.NewGuid();
        UserId = "0";
        GuildId = "0";
        CreatedAt = DateTime.UtcNow;
    }

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

    public ulong? GetUserId()
    {
        if (string.IsNullOrEmpty(UserId)) return null;
        if (ulong.TryParse(UserId, out var result)) return result;
        return null;
    }
    public ulong? GetGuildId()
    {
        if (string.IsNullOrEmpty(GuildId)) return null;
        if (ulong.TryParse(GuildId, out var result)) return result;
        return null;
    }
    public ulong? GetCreatedByUserId()
    {
        if (string.IsNullOrEmpty(CreatedByUserId)) return null;
        if (ulong.TryParse(CreatedByUserId, out var result)) return result;
        return null;
    }
}
