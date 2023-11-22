using Discord;

namespace XeniaBot.DiscordCache.Models;

public class CacheReactionMetadata
{
    public int ReactionCount { get; set; }
    public bool IsMe { get; set; }

    public CacheReactionMetadata Update(ReactionMetadata meta)
    {
        ReactionCount = meta.ReactionCount;
        IsMe = meta.IsMe;
        return this;
    }

    public static CacheReactionMetadata? FromExisting(ReactionMetadata? meta)
    {
        if (meta == null)
            return null;

        var instance = new CacheReactionMetadata();
        return instance.Update((ReactionMetadata)meta);
    }
}