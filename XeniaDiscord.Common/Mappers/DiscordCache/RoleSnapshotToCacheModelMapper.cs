using CSharpFunctionalExtensions;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Shared;
using XeniaDiscord.Data.Models.Cache;
using XeniaDiscord.Data.Models.Snapshot;

namespace XeniaDiscord.Common.Mappers.DiscordCache;

public class RoleSnapshotToCacheModelMapper
    : IMapper<GuildRoleSnapshotModel, GuildRoleCacheModel>
    , IMapperUpdater<GuildRoleSnapshotModel, GuildRoleCacheModel>
    , IMapperUpdater<Maybe<GuildRoleSnapshotModel>, GuildRoleCacheModel>
{
    internal static void RegisterService(IServiceCollection services)
    {
        services.AddSingleton<RoleSnapshotToCacheModelMapper>();
        services.AddSingleton<IMapper<GuildRoleSnapshotModel, GuildRoleCacheModel>>(ServiceGetSelf);
        services.AddSingleton<IMapperUpdater<GuildRoleSnapshotModel, GuildRoleCacheModel>>(ServiceGetSelf);
        services.AddSingleton<IMapperUpdater<Maybe<GuildRoleSnapshotModel>, GuildRoleCacheModel>>(ServiceGetSelf);
    }

    private static RoleSnapshotToCacheModelMapper ServiceGetSelf(IServiceProvider svc)
    {
        return svc.GetRequiredService<RoleSnapshotToCacheModelMapper>();
    }

    public GuildRoleCacheModel Map(GuildRoleSnapshotModel source)
        => MapInternal(source);

    public void Update(GuildRoleCacheModel existing, GuildRoleSnapshotModel mapSource)
    {
        UpdateInternal(existing, mapSource);
    }
    public void Update(GuildRoleCacheModel existing, Maybe<GuildRoleSnapshotModel> mapSource)
    {
        if (mapSource.HasNoValue)
        {
            existing.IsDeleted = true;
            existing.DeletedAt = DateTime.UtcNow;
        }
        else
        {
            UpdateInternal(existing, mapSource.Value);
        }
    }
    private static void UpdateInternal(GuildRoleCacheModel existing, GuildRoleSnapshotModel mapSource)
    {
        existing.Name = mapSource.Name ?? string.Empty;
        existing.Position = mapSource.Position;
        existing.RecordUpdatedAt = mapSource.RecordCreatedAt;
        existing.SnapshotId = mapSource.Id;
    }
    private static GuildRoleCacheModel MapInternal(GuildRoleSnapshotModel? snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var cacheModel = new GuildRoleCacheModel()
        {
            GuildId = snapshot.GuildId,
            RoleId = snapshot.RoleId,
            Name = snapshot.Name ?? string.Empty,
            Position = snapshot.Position,
            RecordCreatedAt = snapshot.RecordCreatedAt,
            RecordUpdatedAt = DateTime.UtcNow,
            SnapshotId = snapshot.Id,
        };
        return cacheModel;
    }
}