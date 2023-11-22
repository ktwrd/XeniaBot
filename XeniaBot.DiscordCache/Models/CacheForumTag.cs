using Discord;
using XeniaBot.DiscordCache.Helpers;

namespace XeniaBot.DiscordCache.Models;

public class CacheForumTag
{
    public ulong ForumTagId { get; set; }
    public string Name { get; set; }
    public CacheEmote? Emoji { get; set; }
    public bool IsModerated { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public CacheForumTag Update(ForumTag tag)
    {
        ForumTagId = tag.Id;
        Name = tag.Name;
        Emoji = CacheEmote.FromExisting(tag.Emoji);
        IsModerated = tag.IsModerated;
        CreatedAt = tag.CreatedAt;
        return this;
    }

    public static CacheForumTag? FromExisting(ForumTag? tag)
    {
        if (tag == null)
            return null;

        var instance = new CacheForumTag();
        return instance.Update((ForumTag)tag);
    }
}