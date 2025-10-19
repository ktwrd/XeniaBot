using System.ComponentModel.DataAnnotations;

namespace XeniaDiscord.Data.Models.Confession;

public class GuildConfessionModel
{
    public const string TableName = "GuildConfession";
    public GuildConfessionModel()
    {
        Id = Guid.NewGuid().ToString();
        GuildId = "0";
        Content = "";

        CreatedByUserId = "0";
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Record Id
    /// </summary>
    [MaxLength(DbGlobals.MaxLength.ULong)]
    public string Id { get; set; }

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
    public GuildConfessionConfigModel? GuildConfessionConfig { get; set; }

    /// <summary>
    /// Content of the confession
    /// </summary>
    [MaxLength(DbGlobals.MaxLength.ULong)]
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
    {
        if (string.IsNullOrEmpty(GuildId))
            return null;
        if (ulong.TryParse(GuildId, out var result) && result > 0)
            return result;
        return null;
    }
    public ulong? GetCreatedByUserId()
    {
        if (string.IsNullOrEmpty(CreatedByUserId))
            return null;
        if (ulong.TryParse(CreatedByUserId, out var result) && result > 0)
            return result;
        return null;
    }
}
