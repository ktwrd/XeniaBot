using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace XeniaDiscord.Data.Models.Warn;

public class GuildWarnStrikeConfigModel : IGuildWarnStrikeConfigModel
{
    public const string TableName = "GuildWarnStrikeConfigs";

    public GuildWarnStrikeConfigModel()
    {
        Id = "0";
        Enabled = false;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        NotificationChannelId = null;
        StrikeCountForNotification = 3;
        StrikeCountForAutokick = null;
        EnableAutokick = false;
        StrikeAliveTime = Convert.ToInt32(Math.Round(TimeSpan.FromDays(14).TotalMinutes));
    }

    /// <inheritdoc/>
    [MaxLength(DbGlobals.MaxLength.ULong)]
    public string Id { get; set; }

    /// <inheritdoc/>
    [DefaultValue(false)]
    public bool Enabled { get; set; }

    /// <inheritdoc/>
    public DateTime CreatedAt { get; set; }

    /// <inheritdoc/>
    public DateTime UpdatedAt { get; set; }

    /// <inheritdoc/>
    [MaxLength(DbGlobals.MaxLength.ULong)]
    public string? NotificationChannelId { get; set; }

    /// <inheritdoc/>
    [DefaultValue(3)]
    public int StrikeCountForNotification { get; set; }

    /// <inheritdoc/>
    public int? StrikeCountForAutokick { get; set; }

    /// <inheritdoc/>
    [DefaultValue(false)]
    public bool EnableAutokick { get; set; }

    /// <inheritdoc/>
    public long StrikeAliveTime { get; set; }


    /// <inheritdoc/>
    public ulong GetGuildId()
        => Id.ParseRequiredULong(nameof(Id), false);

    /// <inheritdoc/>
    public ulong? GetNotificationChannelId()
        => NotificationChannelId?.ParseULong(false);
}
