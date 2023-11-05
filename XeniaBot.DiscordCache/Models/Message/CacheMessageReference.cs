using Discord;

namespace XeniaBot.DiscordCache.Models;

public class CacheMessageReference
{
    public ulong? MessageId { get; set; }
    public ulong? ChannelId { get; set; }
    public ulong? GuildId { get; set; }
    public bool? FailIfNotExists { get; set; }

    public CacheMessageReference Update(MessageReference r)
    {
        MessageId = r.MessageId.GetValueOrDefault();
        ChannelId = r.ChannelId;
        GuildId = r.GuildId.GetValueOrDefault();
        FailIfNotExists = r.FailIfNotExists.GetValueOrDefault();
        return this;
    }
    public static CacheMessageReference? FromExisting(MessageReference? r)
    {
        if (r == null)
            return null;
        
        var instance = new CacheMessageReference();
        return instance.Update(r);
    }
}