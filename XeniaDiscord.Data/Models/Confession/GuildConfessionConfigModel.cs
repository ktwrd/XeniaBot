using System.ComponentModel.DataAnnotations;

namespace XeniaDiscord.Data.Models.Confession;

public class GuildConfessionConfigModel
{
    public const string TableName = "GuildConfessionConfig";
    public GuildConfessionConfigModel()
    {
        Id = "";
        CreatedByUserId = "0";
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Discord Guild Snowflake that this configuration is for.
    /// </summary>
    [MaxLength(DbGlobals.MaxLength.ULong)]
    public string Id { get; set; }

    /// <summary>
    /// Discord Channel Snowflake to put the confession in.
    /// </summary>
    [MaxLength(DbGlobals.MaxLength.ULong)]
    public string? OutputChannelId { get; set; }

    /// <summary>
    /// Discord Channel Snowflake that contains <see cref="ButtonMessageId"/>
    /// </summary>
    [MaxLength(DbGlobals.MaxLength.ULong)]
    public string? ButtonChannelId { get; set; }

    /// <summary>
    /// Discord Message Snowflake that contains the button to confess something.
    /// </summary>
    [MaxLength(DbGlobals.MaxLength.ULong)]
    public string? ButtonMessageId { get; set; }

    /// <summary>
    /// Discord User Snowflake that created this record.
    /// </summary>
    [MaxLength(DbGlobals.MaxLength.ULong)]
    public string CreatedByUserId { get; set; }

    /// <summary>
    /// Time when this record was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }


    public ulong GetGuildId()
        => Id.ParseRequiredULong(nameof(Id), false);
    public ulong? GetOutputChannelId()
        => OutputChannelId?.ParseULong(false);
    public ulong? GetButtonChannelId()
        => ButtonChannelId?.ParseULong(false);
    public ulong? GetButtonMessageId()
        => ButtonMessageId?.ParseULong(false);
    public ulong? GetCreatedByUserId()
        => CreatedByUserId?.ParseULong(false);
}
