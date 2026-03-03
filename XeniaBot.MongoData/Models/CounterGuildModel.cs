using Discord;
using MongoDB.Bson;
using System.ComponentModel;

namespace XeniaBot.MongoData.Models;

public class CounterGuildModel
{
    [Browsable(false)]
    public ObjectId _id { get; set; }
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public ulong Count { get; set; }
    public CounterGuildModel()
        : this(null, null)
    {
    }
    public CounterGuildModel(IChannel? channel, IGuild? guild)
    {
        ChannelId = channel?.Id ?? 0;
        GuildId = guild?.Id ?? 0;
        Count = 0;
    }
}
