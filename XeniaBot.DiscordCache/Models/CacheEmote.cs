using Discord;
using MongoDB.Bson.Serialization.Attributes;

namespace XeniaBot.DiscordCache.Models;

public class CacheEmote
{
    [BsonIgnoreIfNull]
    public string? Name { get; set; }

    public CacheEmote()
    {
        Name = null;
    }
    public static CacheEmote? FromExisting(IEmote? emote)
    {
        if (emote == null)
            return null;

        var instance = new CacheEmote();
        return instance.Update(emote);
    }
    public CacheEmote Update(IEmote emote)
    {
        Name = string.IsNullOrEmpty(emote.Name) ? null : emote.Name;
        return this;
    }
}