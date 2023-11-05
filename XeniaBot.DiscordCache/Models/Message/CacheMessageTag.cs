using Discord;

namespace XeniaBot.DiscordCache.Models;

public class CacheMessageTag : ITag
{
    public int Index { get; set; }
    public int Length { get; set; }
    public TagType Type { get; set; }
    public ulong Key { get; set; }
    public object? Value { get; set; }

    public CacheMessageTag Update(ITag tag)
    {
        Index = tag.Index;
        Length = tag.Length;
        Type = tag.Type;
        Key = tag.Key;
        Value = tag.Value;
        return this;
    }

    public static CacheMessageTag? FromExisting(ITag? tag)
    {
        if (tag == null)
            return null;

        var instance = new CacheMessageTag();
        return instance.Update(tag);
    }
}