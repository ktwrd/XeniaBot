using Microsoft.EntityFrameworkCore;
using NLog;
using XeniaDiscord.Data.Models.Cache;

namespace XeniaDiscord.Data.Repositories;

public class GuildCacheRepository
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();

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
}
