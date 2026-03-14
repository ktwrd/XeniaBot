using Discord;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Shared;
using XeniaDiscord.Data.Models.Cache;

namespace XeniaDiscord.Common.Mappers.DiscordCache;

public class DiscordUserToGuildMemberCacheModelMapper
    : IMapper<IGuildUser, GuildMemberCacheModel>
    , IMapperMerger<IGuildUser, GuildMemberCacheModel>
    , IMapperMerger<IUser, GuildMemberCacheModel>
{
    internal static void RegisterService(IServiceCollection services)
    {
        services.AddSingleton<IMapper<IGuildUser, GuildMemberCacheModel>, DiscordUserToGuildMemberCacheModelMapper>()
                .AddSingleton<IMapperMerger<IGuildUser, GuildMemberCacheModel>, DiscordUserToGuildMemberCacheModelMapper>()
                .AddSingleton<IMapperMerger<IUser, GuildMemberCacheModel>, DiscordUserToGuildMemberCacheModelMapper>();
    }

    public GuildMemberCacheModel Map(IGuildUser source)
        => MapInternal(null, source);

    public GuildMemberCacheModel Map(GuildMemberCacheModel existing, IGuildUser source)
        => MapInternal(existing, source);
    
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="existing"/> is <see langword="null"/>,
    /// and <paramref name="source"/> does not implement <see cref="IGuildUser"/>.
    /// </exception>
    public GuildMemberCacheModel Map(GuildMemberCacheModel existing, IUser source)
        => MapInternal(existing, source);

    private static GuildMemberCacheModel MapInternal(
        GuildMemberCacheModel? existing,
        IUser user)
    {
        var result = new GuildMemberCacheModel()
        {
            UserId = user.Id.ToString(),
            RecordUpdatedAt = DateTime.UtcNow
        };
        if (existing != null)
        {
            result.RecordCreatedAt = existing.RecordCreatedAt;
            result.IsMember = existing.IsMember;
            result.JoinedAt = existing.JoinedAt;
            result.FirstJoinedAt = existing.FirstJoinedAt;
            result.Nickname = existing.Nickname;
        }

        if (user is IGuildUser guildUser)
        {
            result.GuildId = guildUser.GuildId.ToString();
            result.Nickname = string.IsNullOrEmpty(guildUser.Nickname) ? null : guildUser.Nickname;
            result.JoinedAt = guildUser.JoinedAt.HasValue
                ? guildUser.JoinedAt.Value.UtcDateTime
                : null;
        }
        else if (existing == null)
        {
            throw new ArgumentException($"User {user} ({user.Id}) must implement {nameof(IGuildUser)} when parameter {nameof(existing)} is null",
                nameof(user));
        }
        else
        {
            result.IsMember = false;
        }
        result.FirstJoinedAt ??= result.JoinedAt;
        return result;
    }
}
