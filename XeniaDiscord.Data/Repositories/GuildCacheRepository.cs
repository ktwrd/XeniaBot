using CSharpFunctionalExtensions;
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
        if (await db.GuildCache.FindAsync(guildIdStr) != null) return;
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
        await db.GuildCache.AddAsync(model);
    }

    public async Task<Maybe<DateTime>> LastUpdated(XeniaDbContext db, ulong guildId)
    {
        var guildIdStr = guildId.ToString();
        var records = await db.GuildCache.AsNoTracking()
            .Where(e => e.Id == guildIdStr)
            .Select(e => e.RecordUpdatedAt)
            .Take(1)
            .ToArrayAsync();
        if (records.Length == 0) return Maybe.None;
        return records[0];
    }
    
    public async Task InsertOrUpdate(
        XeniaDbContext db,
        GuildCacheModel model)
    {
        if (await db.GuildCache.FindAsync(model.Id) != null)
        {
            if (model.RecordCreatedAt == model.RecordUpdatedAt)
            {
                model.RecordUpdatedAt = DateTime.UtcNow;
            }

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
            _log.Debug($"Updated record (Id={model.Id}, Name={model.Name})");
        }
        else
        {
            await db.GuildCache.AddAsync(model);
            _log.Debug($"Created record (Id={model.Id}, Name={model.Name})");
        }
    }

    public async Task Update(XeniaDbContext db, GuildCacheModel model)
    {
        if (await db.GuildCache.FindAsync(model.Id) == null) return;
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
        _log.Trace($"Updated record (Id={model.Id}, Name={model.Name})");
    }

    public async Task UpdateRoleCache(
        XeniaDbContext db,
        GuildRoleSnapshotModel snapshot,
        DateTime? now = null,
        bool? isDeleted = null)
    {
        var nowValue = now.GetValueOrDefault(DateTime.UtcNow);
        var model = await db.GuildRoleCache.FindAsync(snapshot.RoleId);
        if (model == null)
        {
            var cacheModel = new GuildRoleCacheModel()
            {
                GuildId = snapshot.GuildId,
                RoleId = snapshot.RoleId,
                Name = snapshot.Name ?? string.Empty,
                Position = snapshot.Position,
                RecordCreatedAt = nowValue,
                RecordUpdatedAt = nowValue,
                SnapshotId = snapshot.Id,
            };
            if (isDeleted.HasValue)
            {
                cacheModel.IsDeleted = isDeleted.Value;
                if (cacheModel.IsDeleted) cacheModel.DeletedAt = nowValue;
                else cacheModel.DeletedAt = null;
            }
            await db.GuildRoleCache.AddAsync(cacheModel);
            _log.Debug($"Created record (GuildId={snapshot.GuildId}, RoleId={snapshot.RoleId}, Name={snapshot.Name})");
        }
        else
        {
            await db.GuildRoleCache.Where(e => e.RoleId == snapshot.RoleId)
                .ExecuteUpdateAsync(e => e
                    .SetProperty(p => p.Name, snapshot.Name ?? string.Empty)
                    .SetProperty(p => p.Position, snapshot.Position)
                    .SetProperty(p => p.RecordUpdatedAt, nowValue)
                    .SetProperty(p => p.SnapshotId, snapshot.Id));
            _log.Debug($"Updated record (GuildId={snapshot.GuildId}, RoleId={snapshot.RoleId}, Name={snapshot.Name})");
            if (isDeleted.HasValue)
            {
                DateTime? deletedAtValue = isDeleted.Value ? nowValue : null;
                await db.GuildRoleCache.Where(e => e.RoleId == snapshot.RoleId)
                    .ExecuteUpdateAsync(e => e
                        .SetProperty(p => p.IsDeleted, isDeleted.Value)
                        .SetProperty(p => p.DeletedAt, deletedAtValue));
                _log.Debug($"Marked record as deleted (GuildId={snapshot.GuildId}, RoleId={snapshot.RoleId}, Name={snapshot.Name})");
            }
        }
    }

    public async Task Update(
        XeniaDbContext db,
        GuildRoleCacheModel model)
    {
        if (await db.GuildRoleCache.FindAsync(model.RoleId) == null) return;

        await db.GuildRoleCache.Where(e => e.RoleId == model.RoleId)
            .ExecuteUpdateAsync(e => e
                .SetProperty(p => p.Name, model.Name)
                .SetProperty(p => p.Position, model.Position)
                .SetProperty(p => p.RecordUpdatedAt, model.RecordUpdatedAt)
                .SetProperty(p => p.SnapshotId, model.SnapshotId));
        _log.Debug($"Updated record (GuildId={model.GuildId}, RoleId={model.RoleId}, Name={model.Name})");
    }

    public async Task<MarkRoleAsDeletedResult> MarkRoleAsDeleted(
        XeniaDbContext db,
        ulong roleId,
        DateTime? deletedAt = null)
    {
        var roleIdStr = roleId.ToString();
        var existing = await db.GuildRoleCache.FirstOrDefaultAsync(e => e.RoleId == roleIdStr);
        if (existing == null) return MarkRoleAsDeletedResult.RoleNotFound; // role does not exist in cache
        if (existing.IsDeleted) return MarkRoleAsDeletedResult.RoleAlreadyDeleted;
        var deletedAtValue = deletedAt.GetValueOrDefault(DateTime.UtcNow);
        await db.GuildRoleCache.Where(e => e.RoleId == roleIdStr)
            .ExecuteUpdateAsync(e => e
                .SetProperty(p => p.IsDeleted, true)
                .SetProperty(p => p.DeletedAt, deletedAtValue));
        _log.Debug($"Marked record as deleted (GuildId={existing.GuildId}, RoleId={existing.RoleId}, Name={existing.Name})");
        return MarkRoleAsDeletedResult.Success;
    }

    public enum MarkRoleAsDeletedResult
    {
        Success,
        RoleNotFound,
        RoleAlreadyDeleted
    }
}
