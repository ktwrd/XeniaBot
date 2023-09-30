using Discord;

namespace XeniaBot.DiscordCache.Models;

public class CacheMessageReference
{
    public ulong MessageId { get; set; }
    public ulong ChannelId { get; set; }
    public ulong GuildId { get; set; }
    public bool FailIfNotExists { get; set; }

    public static CacheMessageReference FromMessageReference(MessageReference? r)
    {
        return new CacheMessageReference()
        {
            MessageId = (r?.MessageId.IsSpecified ?? false) ? r?.MessageId.Value ?? 0 : 0,
            ChannelId = r?.ChannelId ?? 0,
            GuildId = (r?.GuildId.IsSpecified ?? false) ? r?.GuildId.Value ?? 0 : 0,
            FailIfNotExists = (r?.FailIfNotExists.IsSpecified ?? false) || (r?.FailIfNotExists.Value ?? true)
        };
    }
}