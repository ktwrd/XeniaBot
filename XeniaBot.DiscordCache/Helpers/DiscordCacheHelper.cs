using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;
using Discord;
using Discord.WebSocket;
using XeniaBot.DiscordCache.Models;
using XeniaBot.DiscordCache.Repositories;
using XeniaBot.Shared.Services;

namespace XeniaBot.DiscordCache.Helpers;

public static class DiscordCacheHelper
{
    public static TH? ForceTypeCast<T, TH>(T input)
    {
        var options = new JsonSerializerOptions()
        {
            IgnoreReadOnlyFields = true,
            IgnoreReadOnlyProperties = true,
            IncludeFields = true,
            ReferenceHandler = ReferenceHandler.Preserve
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

    public static async Task<IUser?> TryGetUser(ulong userId)
    {
        var discord = CoreContext.Instance?.GetRequiredService<DiscordSocketClient>();
        if (discord == null)
            throw new NoNullAllowedException($"Failed to get Service {nameof(DiscordSocketClient)}");
        var discordUser = await discord.GetUserAsync(userId);
        if (discordUser != null)
            return discordUser;

        var repo = CoreContext.Instance?.GetRequiredService<UserCacheRepository>();
        if (repo == null)
            throw new NoNullAllowedException($"Failed to get Service {nameof(UserCacheRepository)}");
        var model = await repo.GetLatest(userId);
        return CacheUserModelData.FromModel(model);
    }
}