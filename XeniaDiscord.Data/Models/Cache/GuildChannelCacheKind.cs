namespace XeniaDiscord.Data.Models.Cache;

[Flags]
public enum GuildChannelCacheKind : uint
{
    None = 0,
    Unknown = 1 << 1,
    Text = 1 << 2,
    Voice = 1 << 3,
    Stage = 1 << 4,
    Thread = 1 << 5,
    News = 1 << 6,
    Category = 1 << 7,
    Forum = 1 << 8,
    Media = 1 << 9
}