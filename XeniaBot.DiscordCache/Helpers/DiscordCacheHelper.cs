using Discord;
using Discord.WebSocket;
using XeniaBot.DiscordCache.Models;
using XeniaBot.DiscordCache.Repositories;
using XeniaBot.Shared.Helpers;

namespace XeniaBot.DiscordCache.Helpers;

public class DiscordCacheHelper(DiscordShardedClient discord, UserCacheRepository userCacheRepo)
{
    public static CacheChannelType GetChannelType<T>(T channel) where T : SocketChannel
    {
        return channel switch
        {
            SocketCategoryChannel => CacheChannelType.Category,
            SocketGroupChannel => CacheChannelType.Group,
            SocketDMChannel => CacheChannelType.DM,
            SocketForumChannel => CacheChannelType.Forum,
            SocketNewsChannel => CacheChannelType.News,
            SocketStageChannel => CacheChannelType.Stage,
            SocketThreadChannel => CacheChannelType.Thread,
            SocketVoiceChannel => CacheChannelType.Voice,
            SocketTextChannel => CacheChannelType.Text,
            _ => CacheChannelType.Unknown
        };
    }

    public async Task<IUser?> TryGetUser(ulong userId)
    {
        var discordUser = ExceptionHelper.RetryOnTimedOut(() => discord.GetUser(userId));
        if (discordUser != null)
            return discordUser;

        var model = await userCacheRepo.GetLatest(userId);
        return CacheUserModelData.FromModel(model);
    }
}