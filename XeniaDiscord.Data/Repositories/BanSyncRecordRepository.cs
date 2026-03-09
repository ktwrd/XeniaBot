using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using XeniaDiscord.Data.Models.BanSync;

namespace XeniaDiscord.Data.Repositories;

public class BanSyncRecordRepository : IDisposable
{
    public void Dispose()
    {
        _serviceScope?.Dispose();
    }
    private readonly IServiceScope? _serviceScope;
    private readonly XeniaDbContext _db;
    public BanSyncRecordRepository(IServiceProvider services)
    {
        _db = services.GetRequiredScopedService<XeniaDbContext>(out _serviceScope);
    }

    public async Task<long> CountAll()
    {
        return await _db.BanSyncRecords.LongCountAsync();
    }
    private static IQueryable<BanSyncRecordModel> ApplyOptions(
        XeniaDbContext db,
        QueryOptions options)
    {
        IQueryable<BanSyncRecordModel> q = db.BanSyncRecords;
        if (options.IgnoreDisabledGuilds)
        {
            q = q.Where(e => e.BanSyncGuild.State == BanSyncGuildState.Active && e.BanSyncGuild.Enable);
        }
        if (!options.IncludeGhostedRecords)
        {
            q = q.Where(e => !e.Ghost);
        }
        q = q.AsNoTracking();
        if (options.IncludeUserPartialSnapshot)
        {
            q = q.Include(e => e.UserPartialSnapshot);
        }
        if (options.IncludeBanSyncGuild)
        {
            q = q.Include(e => e.BanSyncGuild);
        }
        return q;
    }
    public async Task<ICollection<BanSyncRecordModel>> GetAll(
        QueryOptions? options = null,
        PaginationOptions? paginationOptions = null)
    {
        var q = ApplyOptions(_db, options ?? new());
        if (paginationOptions != null) q = q.ApplyPagination(paginationOptions);
        return await q.ToListAsync();
    }

    public async Task<long> CountForGuild(ulong guildId, bool includeGhostedRecords = false)
    {
        var guildIdStr = guildId.ToString();
        var q = _db.BanSyncRecords
            .Where(e => e.GuildId == guildIdStr);
        if (!includeGhostedRecords)
        {
            q = q.Where(e => !e.Ghost);
        }
        return await q.LongCountAsync();
    }

    public async Task<ICollection<BanSyncRecordModel>> GetInfoEnumerable(
        ulong userId, QueryOptions? options = null,
        PaginationOptions? paginationOptions = null)
    {
        var userIdStr = userId.ToString();
        var q = ApplyOptions(_db, options ?? new())
            .Where(e => e.UserId == userIdStr);
        if (paginationOptions != null)
            q = q.ApplyPagination(paginationOptions);
        return await q.ToListAsync();
    }

    public async Task<ICollection<BanSyncRecordModel>> GetInfoEnumerable(
        ulong userId, ulong guildId, QueryOptions? options = null,
        PaginationOptions? paginationOptions = null)
    {
        var userIdStr = userId.ToString();
        var guildIdStr = guildId.ToString();
        var q = ApplyOptions(_db, options ?? new())
            .Where(e => e.UserId == userIdStr && e.GuildId == guildIdStr);
        if (paginationOptions != null)
            q = q.ApplyPagination(paginationOptions);
        return await q.ToListAsync();
    }

    public async Task<BanSyncRecordModel?> GetInfo(ulong userId, ulong guildId, QueryOptions? options = null)
    {
        var userIdStr = userId.ToString();
        var guildIdStr = guildId.ToString();
        return await ApplyOptions(_db, options ?? new())
            .Where(e => e.UserId == userIdStr && e.GuildId == guildIdStr)
            .FirstOrDefaultAsync();
    }
    public async Task<BanSyncRecordModel?> GetInfo(BanSyncRecordModel model, QueryOptions options)
    {
        return await ApplyOptions(_db, options)
            .Where(e => e.Id == model.Id)
            .FirstOrDefaultAsync();
    }
    public Task<BanSyncRecordModel?> GetInfo(string id, QueryOptions? options = null)
    {
        if (!Guid.TryParse(id, out var guidId))
            throw new ArgumentException($"Failed to parse as Guid: {id}", nameof(id));
        return GetInfo(guidId, options);
    }
    public async Task<BanSyncRecordModel?> GetInfo(Guid id, QueryOptions? options = null)
    {
        return await ApplyOptions(_db, options ?? new())
            .Where(e => e.Id == id)
            .FirstOrDefaultAsync();
    }

    private IQueryable<BanSyncRecordModel> GetInfoAllInGuildQuery(
        ulong guildId,
        HashSet<ulong> includedUsers,
        QueryOptions? options = null,
        PaginationOptions? paginationOptions = null)
    {
        var guildIdStr = guildId.ToString();
        var includedUsersStr = includedUsers.Select(e => e.ToString()).ToHashSet();
        var q = ApplyOptions(_db, options ?? new())
            .Where(e => e.GuildId == guildIdStr && includedUsersStr.Contains(e.UserId))
            .OrderByDescending(e => e.CreatedAt);
        if (paginationOptions != null)
        {
            return q.ApplyPagination(paginationOptions);
        }
        return q;
    }
    public async Task<ICollection<BanSyncRecordModel>> GetInfoAllInGuild(
        ulong guildId,
        HashSet<ulong> includedUsers,
        QueryOptions? options = null,
        PaginationOptions? paginationOptions = null)
        => await GetInfoAllInGuildQuery(guildId, includedUsers, options, paginationOptions)
        .ToListAsync();
    public async Task<long> GetInfoAllInGuildCount(
        ulong guildId,
        HashSet<ulong> includedUsers,
        QueryOptions? options = null)
        => await GetInfoAllInGuildQuery(guildId, includedUsers, options)
            .LongCountAsync();
    
    public async Task InsertOrUpdate(BanSyncRecordModel model)
    {
        if (model.GetGuildId() <= 0)
            throw new ArgumentException($"Invalid value {model.GuildId}", $"{nameof(model)}.{nameof(model.GuildId)}");
        if (model.GetUserId() <= 0)
            throw new ArgumentException($"Invalid value {model.UserId}", $"{nameof(model)}.{nameof(model.UserId)}");

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
    public async Task InsertOrUpdate(XeniaDbContext db, BanSyncRecordModel model)
    {
        var previous = await db.BanSyncRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == model.Id);
        if (previous == null)
        {
            await db.BanSyncRecords.AddAsync(model);
        }
        else
        {
            await db.BanSyncRecords
                .Where(e => e.Id == model.Id)
                .ExecuteUpdateAsync(e => e
                .SetProperty(p => p.GuildName, model.GuildName)
                .SetProperty(p => p.Reason, model.Reason)
                .SetProperty(p => p.Ghost, model.Ghost)
                .SetProperty(p => p.Source, model.Source));
        }
    }
    public async Task<bool> Exists(Guid id) => await _db.BanSyncRecords.AnyAsync(e => e.Id == id);
    public async Task SetGhostState(Guid id, bool state)
    {
        await _db.BanSyncRecords.Where(e => e.Id == id)
            .ExecuteUpdateAsync(e => e
            .SetProperty(p => p.Ghost, state));
        await _db.SaveChangesAsync();
    }

    public class QueryOptions
    {
        public bool IncludeUserPartialSnapshot { get; set; } = true;
        public bool IncludeBanSyncGuild { get; set; } = false;
        public bool IncludeGhostedRecords { get; set; } = false;
        public bool IgnoreDisabledGuilds { get; set; } = true;
    }
}
