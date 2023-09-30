using System.Text.Json;
using Discord.WebSocket;
using XeniaBot.DiscordCache.Models;

namespace XeniaBot.DiscordCache.Helpers;

public static class DiscordCacheHelper
{
    public static TH? ForceTypeCast<T, TH>(T input)
    {
        var options = new JsonSerializerOptions()
        {
            IgnoreReadOnlyFields = true,
            IgnoreReadOnlyProperties = true,
            IncludeFields = true
        };
        var teCachet = JsonSerializer.Serialize(input, options);
        var output = JsonSerializer.Deserialize<TH>(teCachet, options);
        return output;
    }

    public static CacheChannelType GetChannelType<T>(T channel) where T : SocketChannel
    {
        if (channel is SocketCategoryChannel)
            return CacheChannelType.Category;
        else if (channel is SocketGroupChannel)
            return CacheChannelType.Group;
        else if (channel is SocketDMChannel)
            return CacheChannelType.DM;
        else if (channel is SocketForumChannel)
            return CacheChannelType.Forum;
        else if (channel is SocketNewsChannel)
            return CacheChannelType.News;
        else if (channel is SocketStageChannel)
            return CacheChannelType.Stage;
        else if (channel is SocketThreadChannel)
            return CacheChannelType.Thread;
        else if (channel is SocketVoiceChannel)
            return CacheChannelType.Voice;
        else if (channel is SocketTextChannel)
            return CacheChannelType.Text;
        else
            return CacheChannelType.Unknown;
    }
}