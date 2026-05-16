using Discord;
using Microsoft.EntityFrameworkCore;
using NLog;
using XeniaDiscord.Data.Models.Cache;
using XeniaDiscord.Data.Models.Snapshot;

namespace XeniaDiscord.Data.Repositories;

public class GuildCacheRepository
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    public async Task Ensure(XeniaDbContext db, ulong guildId, IGuild? guild)
    {
        var guildIdStr = guildId.ToString();
        if (await db.GuildCache.AnyAsync(e => e.Id == guildIdStr)) return;
        var model = new GuildCacheModel()
        {
            Id = guildIdStr,
            Name = guild?.Name,
            CreatedAt = SnowflakeUtils.FromSnowflake(guildId).UtcDateTime,
            OwnerUserId = guild?.OwnerId.ToString(),
            IconUrl = guild?.IconUrl,
            BannerUrl = guild?.BannerUrl,
            SplashUrl = guild?.SplashUrl,
            DiscoverySplashUrl = guild?.DiscoverySplashUrl,
        };
        await InsertOrUpdate(db, model);
    }
    
    
    public async Task InsertOrUpdate(
        XeniaDbContext db,
        GuildCacheModel model)
    {
        model.RecordUpdatedAt = DateTime.UtcNow;
        if (await db.GuildCache.AnyAsync(e => e.Id == model.Id))
        {
            await db.GuildCache.Where(e => e.Id == model.Id)
                .ExecuteUpdateAsync(e => e
                .SetProperty(p => p.Name, model.Name)
                .SetProperty(p => p.OwnerUserId, model.OwnerUserId)
                .SetProperty(p => p.CreatedAt, model.CreatedAt)
                .SetProperty(p => p.JoinedAt, model.JoinedAt)
                .SetProperty(p => p.IconUrl, model.IconUrl)
                .SetProperty(p => p.BannerUrl, model.BannerUrl)
                .SetProperty(p => p.SplashUrl, model.SplashUrl)
                .SetProperty(p => p.DiscoverySplashUrl, model.DiscoverySplashUrl)
                .SetProperty(p => p.RecordUpdatedAt, model.RecordUpdatedAt));
            _log.Debug($"Created record (Id={model.Id}, Name={model.Name})");
        }
        else
        {
            await db.GuildCache.AddAsync(model);
            _log.Debug($"Created record (Id={model.Id}, Name={model.Name})");
        }
    }

    public async Task UpdateRoleCache(
        XeniaDbContext db,
        GuildRoleSnapshotModel snapshot)
    {
        var now = DateTime.UtcNow;
        var model = await db.GuildRoleCache.FindAsync(snapshot.GuildId, snapshot.RoleId);
        if (model == null)
        {
            await db.GuildRoleCache.AddAsync(new GuildRoleCacheModel()
            {
                GuildId = snapshot.GuildId,
                RoleId = snapshot.RoleId,
                Name = snapshot.Name ?? string.Empty,
                Position = snapshot.Position,
                RecordCreatedAt = now,
                RecordUpdatedAt = now,
                SnapshotId = snapshot.Id,
            });
            _log.Debug($"Created record (GuildId={snapshot.GuildId}, RoleId={snapshot.RoleId}, Name={snapshot.Name})");
        }
        else
        {
            await db.GuildRoleCache.Where(e => e.GuildId == snapshot.GuildId && e.RoleId == snapshot.RoleId)
                .ExecuteUpdateAsync(e => e
                    .SetProperty(p => p.Name, snapshot.Name ?? string.Empty)
                    .SetProperty(p => p.Position, snapshot.Position)
                    .SetProperty(p => p.RecordUpdatedAt, now)
                    .SetProperty(p => p.SnapshotId, snapshot.Id));
            _log.Debug($"Updated record (GuildId={snapshot.GuildId}, RoleId={snapshot.RoleId}, Name={snapshot.Name})");
        }
    }
}
