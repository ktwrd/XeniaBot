using Discord;
using XeniaBot.DiscordCache.Helpers;

namespace XeniaBot.DiscordCache.Models;

public class CacheForumTag
{
    public ulong Id { get; set; }
    public string Name { get; set; }
    public CacheEmote? Emoji { get; set; }
    public bool IsModerated { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public CacheForumTag FromForumTag(ForumTag tag)
    {
        Id = tag.Id;
        Name = tag.Name;
        if (tag.Emoji != null)
            Emoji = DiscordCacheHelper.ForceTypeCast<IEmote, CacheEmote>(tag.Emoji);
        IsModerated = tag.IsModerated;
        CreatedAt = tag.CreatedAt;
        return this;
    }
}