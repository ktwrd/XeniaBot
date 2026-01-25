using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace XeniaDiscord.Data.Models.Warn;

public interface IGuildWarnStrikeConfigModel
{
    /// <summary>
    /// Discord Guild Snowflake (ulong as string, use <see cref="GetGuildId"/> for parsing)
    /// </summary>
    [MaxLength(DbGlobals.MaxLength.ULong)]
    public string Id { get; set; }

    /// <summary>
    /// Enable or Disable the Warn Strike system for this guild.
    /// </summary>
    [DefaultValue(false)]
    public bool Enabled { get; set; }

    /// <summary>
    /// Time when this record was created (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Time when this record was updated (UTC)
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// <para>
    /// Discord Channel Snowflake (ulong as string) to use when <see cref="StrikeCountForNotification"/> is triggered.
    /// </para>
    /// Logging for individual warnings can be configured with <see cref="GuildWarnConfigModel.LogChannelId"/>.
    /// </summary>
    [MaxLength(DbGlobals.MaxLength.ULong)]
    public string? NotificationChannelId { get; set; }


    /// <summary>
    /// <para>Maximum strikes allowed for a member.</para>
    /// Default value: <c>3</c>
    /// </summary>
    [DefaultValue(3)]
    public int StrikeCountForNotification { get; set; }

    /// <summary>
    /// Amount of warnings a user needs for autokick to be triggered. Only respected when <see cref="EnableAutokick"/> is set to <see langword="true"/>
    /// </summary>
    /// <remarks>
    /// When this is updated, or <see cref="EnableAutokick"/> is enabled, previous records should not be checked if a user is eligible for autokick.
    /// This is done to prevent major issues from misconfigurations.
    /// </remarks>
    public int? StrikeCountForAutokick { get; set; }

    /// <summary>
    /// Enable autokick user when max strike limit (for autokick) is reached.
    /// Defined by <see cref="StrikeCountForAutokick"/>
    /// </summary>
    [DefaultValue(false)]
    public bool EnableAutokick { get; set; }

    /// <summary>
    /// <para>Maximum age for a warning to contribute to a user's strike count.</para>
    /// <para><b>Measured in minutes.</b></para>
    /// Default value: <c>20_160</c> (14 days)
    /// </summary>
    [DefaultValue(20_160)]
    public long StrikeAliveTime { get; set; }

    public ulong GetGuildId();
    public ulong? GetNotificationChannelId();
}
