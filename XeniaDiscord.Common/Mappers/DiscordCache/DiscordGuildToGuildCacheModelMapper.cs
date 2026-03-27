using Discord;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Shared;
using XeniaDiscord.Data.Models.Cache;

namespace XeniaDiscord.Common.Mappers.DiscordCache;

public class DiscordGuildToGuildCacheModelMapper
    : IMapper<IGuild, GuildCacheModel>
    , IMapperMerger<IGuild, GuildCacheModel>
{
    internal static void RegisterService(IServiceCollection services)
    {
        services.AddSingleton<IMapper<IGuild, GuildCacheModel>, DiscordGuildToGuildCacheModelMapper>()
                .AddSingleton<IMapperMerger<IGuild, GuildCacheModel>, DiscordGuildToGuildCacheModelMapper>();
    }
    public GuildCacheModel Map(IGuild source) => InternalMap(null, source);
    public GuildCacheModel Map(GuildCacheModel existing, IGuild mapSource) => InternalMap(existing, mapSource);
    private static GuildCacheModel InternalMap(GuildCacheModel? existing, IGuild guild)
    {
        ArgumentNullException.ThrowIfNull(guild);

        var result = new GuildCacheModel
        {
            Id = guild.Id.ToString(),
            RecordUpdatedAt = DateTime.UtcNow
        };
        if (existing != null)
        {
            result.Name = existing.Name;
            result.OwnerUserId = existing.OwnerUserId;
            result.CreatedAt = existing.CreatedAt;
            result.JoinedAt = existing.JoinedAt;
            result.IconUrl = existing.IconUrl;
            result.BannerUrl = existing.BannerUrl;
            result.SplashUrl = existing.SplashUrl;
            result.DiscoverySplashUrl = existing.DiscoverySplashUrl;

            result.RecordCreatedAt = existing.RecordCreatedAt;
        }

        result.Name = guild.Name;
        result.OwnerUserId = guild.OwnerId.ToString();
        result.CreatedAt = guild.CreatedAt.UtcDateTime;
        result.JoinedAt ??= DateTime.UtcNow;
        result.IconUrl = guild.IconUrl;
        result.BannerUrl = guild.BannerUrl;
        result.SplashUrl = guild.SplashUrl;
        result.DiscoverySplashUrl = guild.DiscoverySplashUrl;

        return result;
    }
}
