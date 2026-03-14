using Discord;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Shared;
using XeniaDiscord.Data.Models.Cache;

namespace XeniaDiscord.Common.Mappers.DiscordCache;

public class DiscordUserToUserCacheModelMapper
    : IMapper<IUser, UserCacheModel>
    , IMapperMerger<IUser, UserCacheModel>
{
    internal static void RegisterService(IServiceCollection services)
    {
        services.AddSingleton<IMapper<IUser, UserCacheModel>, DiscordUserToUserCacheModelMapper>()
                .AddSingleton<IMapperMerger<IUser, UserCacheModel>, DiscordUserToUserCacheModelMapper>();
    }

    public UserCacheModel Map(IUser source) => MapInternal(source);
    public UserCacheModel Map(UserCacheModel existing, IUser mapSource)=> MapInternal(existing, mapSource);
    
    private static UserCacheModel MapInternal(IUser? user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return MapInternal(null, user);
    }
    
    private static UserCacheModel MapInternal(UserCacheModel? existing, IUser? user)
    {
        ArgumentNullException.ThrowIfNull(user);
        var result = new UserCacheModel();
        if (existing == null)
        {
            result.Id = user.Id.ToString();
            result.CreatedAt = user.CreatedAt.UtcDateTime;
        }
        else
        {
            result.Id = existing.Id;
            result.RecordCreatedAt = existing.RecordCreatedAt;
        }

        result.Username = user.Username;
        result.Discriminator = MapDiscriminator(user);
        result.GlobalName = MapGlobalName(user);
        result.RecordUpdatedAt = DateTime.UtcNow;
        // doesn't matter if an exception is thrown
        try
        {
            result.DisplayAvatarUrl = user.GetDisplayAvatarUrl();
        }
        catch { }

        return result;
    }

    private static string? MapDiscriminator(IUser user)
    {
        if (user.DiscriminatorValue == 0 ||
            string.IsNullOrEmpty(user.Discriminator.Trim().Trim('0')))
        {
            return null;
        }
        return user.Discriminator;
    }
    private static string? MapGlobalName(IUser user)
    {
        return string.IsNullOrEmpty(user.GlobalName?.Trim())
            ? null
            : user.GlobalName;
    }
}
