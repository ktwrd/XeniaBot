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
    public new CacheTextChannelModel FromExisting(SocketTextChannel channel)
    {
        base.FromExisting(channel);
        Topic = channel.Topic;
        SlowModeInterval = channel.SlowModeInterval;
        CategoryId = channel.CategoryId;
        IsNsfw = channel.IsNsfw;
        DefaultArchiveDuration = channel.DefaultArchiveDuration;
        Mention = channel.Mention;
        ThreadIds = channel.Threads.Select(v => v.Id).ToArray();
        return this;
    }
}