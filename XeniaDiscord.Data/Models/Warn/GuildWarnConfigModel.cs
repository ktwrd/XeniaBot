using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace XeniaDiscord.Data.Models.Warn;

public class GuildWarnConfigModel
{
    public const string TableName = "GuildWarnConfigs";

    public GuildWarnConfigModel()
    {
        Id = "0";
        CreatedAt = DateTime.UtcNow;
        CreatedByUserId = "0";
        UpdatedAt = CreatedAt;
        UpdatedByUserId = "0";
        LogChannelId = null;
        EnableLogging = false;
    }

    /// <summary>
    /// Discord Guild Snowflake (ulong as string, use <see cref="GetGuildId"/> for parsing)
    /// </summary>
    [MaxLength(DbGlobals.MaxLength.ULong)]
    public string Id { get; set; }

    /// <summary>
    /// Time when this record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Discord User Snowflake that created this record.
    /// </summary>
    [MaxLength(DbGlobals.MaxLength.ULong)]
    public string CreatedByUserId { get; set; }

    /// <summary>
    /// Time when this record was updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
    /// <summary>
    /// Discord User Snowflake that updated this record.
    /// </summary>
    [MaxLength(DbGlobals.MaxLength.ULong)]
    public string UpdatedByUserId { get; set; }

    /// <summary>
    /// Discord Channel Id (ulong as string, use <see cref="GetLogChannelId"/> for parsing)
    /// </summary>
    [MaxLength(DbGlobals.MaxLength.ULong)]
    public string? LogChannelId { get; set; }

    /// <summary>
    /// Should warn logging be enabled?
    /// </summary>
    [DefaultValue(false)]
    public bool EnableLogging { get; set; }

    public ulong GetGuildId()
        => Id.ParseRequiredULong(nameof(Id), false);
    public ulong? GetCreatedByUserId()
        => CreatedByUserId?.ParseULong(false);
    public ulong? GetUpdatedByUserId()
        => UpdatedByUserId?.ParseULong(false);
    public ulong? GetLogChannelId()
        => LogChannelId?.ParseULong(false);
}
