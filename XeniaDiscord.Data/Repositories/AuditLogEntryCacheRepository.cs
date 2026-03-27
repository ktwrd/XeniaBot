using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using XeniaDiscord.Data.Models.Cache;

namespace XeniaDiscord.Data.Repositories;

public class AuditLogEntryCacheRepository
{
    public async Task InsertOrUpdate(XeniaDbContext db, AuditLogBanCacheModel model)
    {
        await InsertOrUpdateInternal(
            db.AuditLogBanEntryCache,
            model,
            e => e
            .SetProperty(p => p.TargetUserId, model.TargetUserId));
    }
    private static async Task InsertOrUpdateInternal<TModel>(
        DbSet<TModel> dbSet,
        TModel model,
        Action<UpdateSettersBuilder<TModel>>? executeUpdateCallback)
        where TModel : BaseAuditLogEntryCacheModel
    {
        if (await dbSet.AnyAsync(e => e.Id == model.Id))
        {
            model.RecordUpdatedAt = DateTime.UtcNow;
            await dbSet.Where(e => e.Id == model.Id)
                .ExecuteUpdateAsync(e => e
                .SetProperty(p => p.GuildId, model.GuildId)
                .SetProperty(p => p.CreatedAt, model.CreatedAt)
                .SetProperty(p => p.Action, model.Action)
                .SetProperty(p => p.PerformedByUserId, model.PerformedByUserId)
                .SetProperty(p => p.Reason, model.Reason)
                .SetProperty(p => p.JsonData, model.JsonData)
                .SetProperty(p => p.JsonDataType, model.JsonDataType)
                .SetProperty(p => p.RecordUpdatedAt, model.RecordUpdatedAt));
            if (executeUpdateCallback != null)
            {
                await dbSet.Where(e => e.Id == model.Id)
                    .ExecuteUpdateAsync(executeUpdateCallback);
            }
        }
        else
        {
            await dbSet.AddAsync(model);
        }
    }
}
