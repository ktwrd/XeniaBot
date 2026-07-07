using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using XeniaDiscord.Data.Models.BanSync;

namespace XeniaDiscord.Data.Repositories;

public class BanSyncGuildSnapshotRepository
{
    private readonly IDbContextFactory<XeniaDbContext> _dbContextFactory;
    public BanSyncGuildSnapshotRepository(IServiceProvider services)
    {
        _dbContextFactory = services.GetRequiredService<IDbContextFactory<XeniaDbContext>>();
    }

    public async Task<ICollection<BanSyncGuildSnapshotModel>> GetMany(ulong guildId)
    {
        var guildIdStr = guildId.ToString();
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        return await db.BanSyncGuildSnapshots.AsNoTracking()
            .Where(e => e.GuildId == guildIdStr)
            .OrderByDescending(e => e.Timestamp)
            .ToListAsync();
    }
}
