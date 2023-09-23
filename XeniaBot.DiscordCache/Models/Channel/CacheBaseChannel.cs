using XeniaBot.DiscordCache.Helpers;
using Discord.WebSocket;
using XeniaBot.Shared.Models;

namespace XeniaBot.DiscordCache.Models;

public abstract class CacheBaseChannel
{
    public ulong Snowflake { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsGuildChannel { get; set; }
    public bool IsDmChannel { get; set; }
    public bool IsForumChannel { get; set; }
    public CacheChannelType Type { get; set; }
    
    public CacheBaseChannel FromExisting(SocketChannel channel)
    {
        Snowflake = channel.Id;
        CreatedAt = channel.CreatedAt;
        Type = DiscordCacheHelper.GetChannelType(channel);
        return this;
    }
}