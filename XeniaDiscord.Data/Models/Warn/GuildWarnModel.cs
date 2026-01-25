using System.ComponentModel.DataAnnotations;

namespace XeniaDiscord.Data.Models.Warn;

public class GuildWarnModel
{
    public const string TableName = "GuildWarns";
    public GuildWarnModel()
    {
        Id = Guid.NewGuid();
        GuildId = "";
        TargetUserId = "";
        Description = "";
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Record Guid
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Discord Guild Snowflake for what guild this user was warned in.
    /// Foreign Key to <see cref="GuildWarnConfigModel.Id"/>
    /// </summary>
    [MaxLength(DbGlobals.MaxLength.ULong)]
    public string GuildId { get; set; }

    /// <summary>
    /// Discord User Snowflake for who was warned (ulong as string, use <see cref="GetTargetUserId"/>)
    /// </summary>
    [MaxLength(DbGlobals.MaxLength.ULong)]
    public string TargetUserId { get; set; }

    [Required]
    [MaxLength(DbGlobals.MaxStringSize)]
    public string Description { get; set; }

    /// <summary>
    /// Discord User Snowflake for who created this warning (ulong as string, use <see cref="GetCreatedByUserId"/>)
    /// </summary>
    [MaxLength(DbGlobals.MaxLength.ULong)]
    public string? CreatedByUserId { get; set; }

    /// <summary>
    /// Time when this record was created (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; }

    public ulong GetGuildId()
        => GuildId.ParseRequiredULong(nameof(GuildId), false);
    public ulong? GetTargetUserId()
        => TargetUserId?.ParseULong(false);
    public ulong? GetCreatedByUserId()
        => CreatedByUserId?.ParseULong(false);
}
