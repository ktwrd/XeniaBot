using System.ComponentModel.DataAnnotations;

namespace XeniaDiscord.Data.Models.Confession;

public class GuildConfessionModel
{
    public const string TableName = "GuildConfession";
    public GuildConfessionModel()
    {
        Id = Guid.NewGuid();
        GuildId = "0";
        Content = "";
        CreatedByUserId = "0";
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Record Id
    /// </summary>
    [MaxLength(DbGlobals.MaxLength.ULong)]
    public Guid Id { get; set; }

    /// <summary>
    /// Discord Guild Snowflake (ulong as string).
    /// Will probably be related to <see cref="GuildConfessionConfigModel.DiscordGuildId"/>
    /// </summary>
    [MaxLength(DbGlobals.MaxLength.ULong)]
    public string GuildId { get; set; }

    /// <summary>
    /// Optional Foreign Key to <see cref="GuildConfessionConfigModel.Id"/>
    /// </summary>
    [MaxLength(DbGlobals.MaxLength.ULong)]
    public string? GuildConfessionConfigId { get; set; }

    /// <summary>
    /// Content of the confession
    /// </summary>
    [MaxLength(DbGlobals.MaxLength.Confession)]
    [MinLength(1)]
    public string Content { get; set; }

    /// <summary>
    /// Discord User Snowflake that created this confession (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.MaxLength.ULong)]
    public string CreatedByUserId { get; set; }

    /// <summary>
    /// When this confession was created (UTC)
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    public ulong? GetGuildId()
        => GuildId.ParseRequiredULong(nameof(GuildId), false);
    public ulong? GetCreatedByUserId()
        => GuildId.ParseULong(false);

    #region Property Accessors
    public GuildConfessionConfigModel? GuildConfessionConfig { get; set; }
    #endregion
}
