using Discord;
using MongoDB.Bson.Serialization.Attributes;
using System.Globalization;
using System.Xml.Linq;

namespace XeniaBot.DiscordCache.Models;

public class CacheEmote : ICacheEmote
{
    [BsonIgnoreIfNull]
    public string? Name { get; set; }

    /// <summary>
    /// ulong stored as string
    /// </summary>
    [BsonIgnoreIfNull]
    public string? Id { get; set; }

    [BsonIgnoreIfNull]
    public bool? Animated { get; set; }

    public CacheEmote()
    {
        Name = null;
        Id = null;
        Animated = null;
    }
    
    public static CacheEmote? FromExisting(IEmote? emote)
    {
        if (emote == null)
            return null;

        var instance = new CacheEmote();
        instance.Update(emote);
        return instance;
    }
    
    public void Update(IEmote emote)
    {
        this.UpdateValues(emote);
    }
}

public static class CacheEmoteExtensions
{
    public static void UpdateValues(this ICacheEmote instance, IEmote? emote)
    {
        instance.Name = string.IsNullOrEmpty(emote?.Name) ? null : emote.Name;
        if (emote is Emote emoteClass)
        {
            instance.Id = emoteClass.Id.ToString("D", CultureInfo.InvariantCulture);
            instance.Animated = emoteClass.Animated;
        }
    }
}

public interface ICacheEmote
{
    [BsonIgnoreIfNull]
    public string? Name { get; set; }

    /// <summary>
    /// ulong stored as string
    /// </summary>
    [BsonIgnoreIfNull]
    public string? Id { get; set; }

    [BsonIgnoreIfNull]
    public bool? Animated { get; set; }
    public void Update(IEmote emote);
}