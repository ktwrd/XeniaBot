using Discord;

namespace XeniaBot.DiscordCache.Models;

public class CacheMessageTag : ITag
{
    public int Index { get; set; }
    public int Length { get; set; }
    public TagType Type { get; set; }
    public ulong Key { get; set; }
    public object Value { get; set; }
}