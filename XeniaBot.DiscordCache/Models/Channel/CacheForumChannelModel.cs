using Discord;
using Discord.WebSocket;

namespace XeniaBot.DiscordCache.Models;

public class CacheForumChannelModel : CacheGuildChannelModel
{
    public string Topic { get; set; }
    public bool IsNsfw { get; set; }
    public ThreadArchiveDuration DefaultAutoArchiveDuration { get; set; }
    public CacheForumTag[] Tags { get; set; }
    public int ThreadCreationInterval { get; set; }
    public int DefaultSlowModeInterval { get; set; }
    public CacheEmote? DefaultReactionEmoji { get; set; }
    public ForumSortOrder? DefaultSortOrder { get; set; }
    public ForumLayout DefaultLayout { get; set; }

    public new CacheForumChannelModel Update(SocketForumChannel channel)
    {
        base.Update(channel);
        IsNsfw = channel.IsNsfw;
        Topic = channel.Topic;
        DefaultAutoArchiveDuration = channel.DefaultAutoArchiveDuration;
        var tagList = new List<CacheForumTag>();
        foreach (var i in channel.Tags)
        {
            tagList.Add(new CacheForumTag().Update(i));
        }

        Tags = tagList.ToArray();
        ThreadCreationInterval = channel.ThreadCreationInterval;
        DefaultSlowModeInterval = channel.DefaultSlowModeInterval;
        DefaultReactionEmoji = CacheEmote.FromExisting(channel.DefaultReactionEmoji);
        DefaultSortOrder = channel.DefaultSortOrder;
        DefaultLayout = channel.DefaultLayout;
        IsForumChannel = true;
        return this;
    }

    public static CacheForumChannelModel? FromExisting(SocketForumChannel? channel)
    {
        if (channel == null)
            return null;

        var instance = new CacheForumChannelModel();
        return instance.Update(channel);
    }
}