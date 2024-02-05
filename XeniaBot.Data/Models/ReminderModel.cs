using kate.shared.Helpers;
using MongoDB.Bson.Serialization.Serializers;
using XeniaBot.Shared.Models;

namespace XeniaBot.Data.Models;

public class ReminderModel : BaseModel
{
    public string ReminderId { get; set; }
    /// <summary>
    /// User that created this reminder.
    /// </summary>
    public ulong UserId { get; set; }
    /// <summary>
    /// Guild where this reminder was created in.
    /// </summary>
    public ulong GuildId { get; set; }
    /// <summary>
    /// Channel where the user should be pinged in.
    /// </summary>
    public ulong ChannelId { get; set; }
    /// <summary>
    /// Timestamp that this reminder was created at.
    ///
    /// Milliseconds since Unix Epoch (UTC)
    /// </summary>
    public long CreatedAt { get; set; }
    /// <summary>
    /// Timestamp when we should notify the user at.
    ///
    /// Seconds since Unix Epoch (UTC)
    /// </summary>
    public long ReminderTimestamp { get; set; }
    /// <summary>
    /// Have we notified the user of this reminder?
    ///
    /// Ignore sending notification/showing user when this is `true`.
    /// </summary>
    public bool HasReminded { get; set; }
    /// <summary>
    /// Note created by user.
    /// </summary>
    public string Note { get; set; }
    /// <summary>
    /// Timestamp when we notified the user of this reminder.
    ///
    /// Milliseconds since Unix Epoch (UTC)
    /// </summary>
    public long RemindedAt { get; set; }
    /// <summary>
    /// Where was this reminder created? Bot or Dashboard.
    /// </summary>
    public RemindSource Source { get; set; }

    public ReminderModel()
    {
        ReminderId = GeneralHelper.GenerateUID();
        HasReminded = false;
        Note = "";
        CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    public ReminderModel(
        ulong userId,
        ulong channelId,
        ulong guildId,
        long timestamp,
        RemindSource source,
        string? note = null)
    : base()
    {
        ReminderId = GeneralHelper.GenerateUID();
        UserId = userId;
        ChannelId = channelId;
        GuildId = guildId;
        ReminderTimestamp = timestamp;
        Note = note ?? "";
        HasReminded = false;
        Source = source;
    }
}

public enum RemindSource
{
    Unknown = -1,
    Bot = 0,
    Dashboard = 1
}