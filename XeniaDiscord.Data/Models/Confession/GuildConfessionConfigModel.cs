using System.ComponentModel.DataAnnotations;

namespace XeniaDiscord.Data.Models.Confession;

public class GuildConfessionConfigModel
{
    public const string TableName = "GuildConfessionConfig";
    public GuildConfessionConfigModel()
    {
        Id = "";

        OutputChannelId = "";
        ButtonChannelId = "";
        ButtonMessageId = "";

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
    public string OutputChannelId { get; set; }

    /// <summary>
    /// Discord Channel Snowflake that contains <see cref="ButtonMessageId"/>
    /// </summary>
    [MaxLength(DbGlobals.MaxLength.ULong)]
    public string ButtonChannelId { get; set; }

    /// <summary>
    /// Discord Message Snowflake that contains the button to confess something.
    /// </summary>
    [MaxLength(DbGlobals.MaxLength.ULong)]
    public string ButtonMessageId { get; set; }

    /// <summary>
    /// Discord User Snowflake that created this record.
    /// </summary>
    [MaxLength(DbGlobals.MaxLength.ULong)]
    public string CreatedByUserId { get; set; }

    /// <summary>
    /// Time when this record was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }


    public ulong? GetGuildId()
    {
        if (string.IsNullOrEmpty(Id))
            return null;
        if (ulong.TryParse(Id, out var result) && result > 0)
            return result;
        return null;
    }
    public ulong? GetOutputChannelId()
    {
        if (!string.IsNullOrEmpty(OutputChannelId) &&
            ulong.TryParse(OutputChannelId, out var result) && result > 0)
            return result;
        return null;
    }
    public ulong? GetButtonChannelId()
    {
        if (!string.IsNullOrEmpty(ButtonChannelId) &&
            ulong.TryParse(ButtonChannelId, out var result) && result > 0)
            return result;
        return null;
    }
    public ulong? GetButtonMessageId()
    {
        if (!string.IsNullOrEmpty(ButtonMessageId) &&
            ulong.TryParse(ButtonMessageId, out var result) && result > 0)
            return result;
        return null;
    }
    public ulong? GetCreatedByUserId()
    {
        if (!string.IsNullOrEmpty(CreatedByUserId) &&
            ulong.TryParse(CreatedByUserId, out var result) && result > 0)
            return result;
        return null;
    }
}
