using Discord;

namespace XeniaBot.DiscordCache.Models;

public class CacheCustomSticker : CacheSticker
{
    public ulong? AuthorId { get; set; }
    public ulong GuildId { get; set; }

    public CacheCustomSticker FromExisting(ICustomSticker sticker)
    {
        base.FromExisting(sticker);
        AuthorId = sticker.AuthorId;
        GuildId = sticker.Guild.Id;
        return this;
    }
}