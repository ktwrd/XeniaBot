using Discord;
using Discord.WebSocket;

namespace XeniaBot.DiscordCache.Models;

public class CacheTextChannelModel : CacheGuildChannelModel
{
    public string Topic { get; set; }
    public int SlowModeInterval { get; set; }
    public ulong? CategoryId { get; set; }
    public bool IsNsfw { get; set; }
    public ThreadArchiveDuration DefaultArchiveDuration { get; set; }
    public string Mention { get; set; }
    public ulong[] ThreadIds { get; set; }
    public new CacheTextChannelModel Update(SocketTextChannel channel)
    {
        base.Update(channel);
        Topic = channel.Topic;
        SlowModeInterval = channel.SlowModeInterval;
        CategoryId = channel.CategoryId;
        IsNsfw = channel.IsNsfw;
        DefaultArchiveDuration = channel.DefaultArchiveDuration;
        Mention = channel.Mention;
        ThreadIds = channel.Threads.Select(v => v.Id).ToArray();
        return this;
    }

    public static CacheTextChannelModel? FromExisting(SocketTextChannel? channel)
    {
        if (channel == null)
            return null;

        var instance = new CacheTextChannelModel();
        return instance.Update(channel);
    }
}