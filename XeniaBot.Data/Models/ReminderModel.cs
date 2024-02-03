using kate.shared.Helpers;
using MongoDB.Bson.Serialization.Serializers;
using XeniaBot.Shared.Models;

namespace XeniaBot.Data.Models;

public class ReminderModel : BaseModel
{
    public string ReminderId { get; set; }
    public ulong UserId { get; set; }
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public long CreatedAt { get; set; }
    public long ReminderTimestamp { get; set; }
    public bool HasReminded { get; set; }
    public string Note { get; set; }
    public long RemindedAt { get; set; }
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