using Discord;
using MongoDB.Bson.Serialization.Attributes;

namespace XeniaBot.DiscordCache.Models;

public class CacheCustomSticker : CacheSticker
{
    [BsonIgnoreIfNull]
    public ulong? AuthorId { get; set; }
    public ulong GuildId { get; set; }
    public CacheCustomSticker Update(ICustomSticker sticker)
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