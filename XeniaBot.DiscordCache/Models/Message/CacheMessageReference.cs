using Discord;

namespace XeniaBot.DiscordCache.Models;

public class CacheMessageReference
{
    public ulong? MessageId { get; set; }
    public ulong? ChannelId { get; set; }
    public ulong? GuildId { get; set; }
    public bool? FailIfNotExists { get; set; }

    public static CacheMessageReference? FromMessageReference(MessageReference? r)
    {
        if (r == null)
            return null;
        
        var instance = new CacheMessageReference();
        if (r?.MessageId.IsSpecified ?? false)
        {
            instance.MessageId = r?.MessageId.Value ?? 0;
        }
        else
        {
            instance.MessageId = null;
        }

        instance.ChannelId = r?.ChannelId;
        
        if (r?.GuildId.IsSpecified ?? false)
        {
            instance.GuildId = r.GuildId.Value;
        }
        else
        {
            instance.GuildId = null;
        }

        instance.FailIfNotExists = (r?.FailIfNotExists.IsSpecified ?? false) || (r?.FailIfNotExists.Value ?? true);
        return instance;
    }
}