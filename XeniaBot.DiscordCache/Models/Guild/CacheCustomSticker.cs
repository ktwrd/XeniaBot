using Discord;

namespace XeniaBot.DiscordCache.Models;

public class CacheCustomSticker : CacheSticker
{
    public ulong? AuthorId { get; set; }
    public ulong GuildId { get; set; }
    public new CacheCustomSticker Update(ICustomSticker sticker)
    {
        base.Update(sticker);
        AuthorId = sticker.AuthorId;
        GuildId = sticker.Guild.Id;
        return this;
    }
    public static CacheCustomSticker? FromExisting(ICustomSticker? sticker)
    {
        if (sticker == null)
            return null;

        var instance = new CacheCustomSticker();
        return instance.Update(sticker);
    }
}