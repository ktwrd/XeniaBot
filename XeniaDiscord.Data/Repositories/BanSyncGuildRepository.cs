using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using XeniaDiscord.Data.Models.BanSync;

namespace XeniaDiscord.Data.Repositories;

public class BanSyncGuildRepository : IDisposable
{
    public void Dispose()
    {
        _serviceScope?.Dispose();
    }
    private readonly IServiceScope? _serviceScope;
    private readonly XeniaDbContext _db;
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    public BanSyncGuildRepository(IServiceProvider services)
    {
        _db = services.GetRequiredScopedService<XeniaDbContext>(out _serviceScope);
    }

    public async Task<long> CountAll()
    {
        await using var db = _db.CreateSession();
        return await CountAll(db);
    }
    public async Task <long> CountAll(XeniaDbContext db)
    {
        return await db.BanSyncGuilds.LongCountAsync();
    }
    public async Task<bool> Exists(ulong guildId)
    {
        await using var db = _db.CreateSession();
        return await Exists(db, guildId);
    }
    public async Task<bool> Exists(XeniaDbContext db, ulong guildId)
    {
        var guildIdStr = guildId.ToString();
        return await db.BanSyncGuilds.AnyAsync(e => e.GuildId == guildIdStr);
    }
    
    public async Task<BanSyncGuildModel?> GetAsync(ulong guildId)
    {
        await using var db = _db.CreateSession();
        return await GetAsync(db, guildId);
    }
    public async Task<BanSyncGuildModel?> GetAsync(XeniaDbContext db, ulong guildId)
    {
        var guildIdStr = guildId.ToString();
        return await db.BanSyncGuilds
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.GuildId == guildIdStr);
    }
    

    public async Task InsertOrUpdate(BanSyncGuildModel model)
    {
        if (model.GetGuildId() <= 1)
            throw new ArgumentException($"Invalid value {model.GuildId}", $"{nameof(model)}.{nameof(model.GuildId)}");

        await using var db = _db.CreateSession();
        await using var trans = await db.Database.BeginTransactionAsync();
        try
        {
            await InsertOrUpdate(db, model);
            await db.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }
    }
    public async Task InsertOrUpdate(
        XeniaDbContext db,
        BanSyncGuildModel model)
    {
        var previous = await db.BanSyncGuilds
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.GuildId == model.GuildId);
        if (previous == null)
        {
            await db.BanSyncGuilds.AddAsync(model);
            await db.BanSyncGuildSnapshots.AddAsync(new BanSyncGuildSnapshotModel(model));
        }
        else
        {
            await db.BanSyncGuildSnapshots.AddAsync(new BanSyncGuildSnapshotModel(model));
            var count = await db.BanSyncGuilds
                .Where(e => e.GuildId == model.GuildId)
                .ExecuteUpdateAsync(e => e
                .SetProperty(p => p.LogChannelId, model.LogChannelId)
                .SetProperty(p => p.Enable, model.Enable)
                .SetProperty(p => p.State, model.State)
                .SetProperty(p => p.Notes, model.Notes));
            _log.Debug($"Updated {count} records for GuildId={model.GuildId}");
        }
    }
}
