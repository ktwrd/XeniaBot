using Microsoft.EntityFrameworkCore;
using NLog;
using XeniaDiscord.Data.Models.Cache;

namespace XeniaDiscord.Data.Repositories;

public class GuildMemberCacheRepository
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();

    public async Task InsertOrUpdate(
        XeniaDbContext db,
        GuildMemberCacheModel model)
    {
        model.RecordUpdatedAt = DateTime.UtcNow;
        if (await db.GuildMemberCache.AnyAsync(e => e.GuildId == model.GuildId && e.UserId == model.UserId))
        {
            await db.GuildMemberCache.Where(e => e.GuildId == model.GuildId && e.UserId == model.UserId)
                .ExecuteUpdateAsync(e => e
                .SetProperty(p => p.IsMember, model.IsMember)
                .SetProperty(p => p.JoinedAt, model.JoinedAt)
                .SetProperty(p => p.IsBot, model.IsBot)
                .SetProperty(p => p.IsWebhook, model.IsWebhook)
                .SetProperty(p => p.FirstJoinedAt, model.FirstJoinedAt)
                .SetProperty(p => p.Nickname, model.Nickname)
                .SetProperty(p => p.RecordUpdatedAt, model.RecordUpdatedAt));
            _log.Debug($"Updated record (Id={model.UserId}, GuildId={model.GuildId})");
        }
        else
        {
            await db.GuildMemberCache.AddAsync(model);
            _log.Debug($"Created record (Id={model.UserId}, GuildId={model.GuildId})");
        }
    }
}
