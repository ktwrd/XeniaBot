using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using XeniaDiscord.Data.Models.BanSync;

namespace XeniaDiscord.Data.Repositories;

public class BanSyncGuildSnapshotRepository : IDisposable
{
    public void Dispose()
    {
        _serviceScope?.Dispose();
    }
    private readonly IServiceScope? _serviceScope;
    private readonly XeniaDbContext _db;
    public BanSyncGuildSnapshotRepository(IServiceProvider services)
    {
        _db = services.GetRequiredScopedService<XeniaDbContext>(out _serviceScope);
    }

    public async Task<ICollection<BanSyncGuildSnapshotModel>> GetMany(ulong guildId)
    {
        var guildIdStr = guildId.ToString();
        return await _db.BanSyncGuildSnapshots.AsNoTracking()
            .Where(e => e.GuildId == guildIdStr)
            .OrderByDescending(e => e.Timestamp)
            .ToListAsync();
    }
}
