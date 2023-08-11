using kate.shared.Helpers;
using MongoDB.Bson.Serialization.Serializers;

namespace XeniaBot.Core.Models;

public class ReminderModel : BaseModel
{
    public string ReminderId { get; set; }
    public ulong UserId { get; set; }
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public long ReminderTimestamp { get; set; }
    public bool HasReminded { get; set; }
    public string Note { get; set; }

    public ReminderModel()
    {
        ReminderId = GeneralHelper.GenerateUID();
        HasReminded = false;
        Note = "";
    }

    public ReminderModel(
        ulong userId,
        ulong channelId,
        ulong guildId,
        long timestamp,
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
    }
}