using Microsoft.EntityFrameworkCore;
using NLog;
using XeniaDiscord.Data.Models.Cache;

namespace XeniaDiscord.Data.Repositories;

public class UserCacheRepository
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    
    public async Task InsertOrUpdate(
        XeniaDbContext db,
        UserCacheModel model)
    {
        model.RecordUpdatedAt = DateTime.UtcNow;
        if (await db.UserCache.AnyAsync(e => e.Id == model.Id))
        {
            await db.UserCache.Where(e => e.Id == model.Id)
                .ExecuteUpdateAsync(e => e
                .SetProperty(p => p.Username, model.Username)
                .SetProperty(p => p.Discriminator, model.Discriminator)
                .SetProperty(p => p.GlobalName, model.GlobalName)
                .SetProperty(p => p.DisplayAvatarUrl, model.DisplayAvatarUrl)
                .SetProperty(p => p.IsBot, model.IsBot)
                .SetProperty(p => p.IsWebhook, model.IsWebhook)
                .SetProperty(p => p.RecordUpdatedAt, model.RecordUpdatedAt));
            _log.Debug($"Updated record (Id={model.Id}, Username={model.Username})");
        }
        else
        {
            await db.UserCache.AddAsync(model);
            _log.Debug($"Created record (Id={model.Id}, Username={model.Username})");
        }
    }
}
