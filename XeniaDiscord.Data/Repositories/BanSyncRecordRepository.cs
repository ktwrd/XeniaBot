using System.Globalization;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using XeniaDiscord.Data.Models.BanSync;
// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable RedundantDefaultMemberInitializer
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBeMadeStatic.Global
// ReSharper disable ConvertToPrimaryConstructor
#pragma warning disable CA1822

namespace XeniaDiscord.Data.Repositories;

public class BanSyncRecordRepository
{
    private readonly IDbContextFactory<XeniaDbContext> _dbContextFactory;
    public BanSyncRecordRepository(IServiceProvider services)
    {
        _dbContextFactory = services.GetRequiredService<IDbContextFactory<XeniaDbContext>>();
    }

    public async Task<long> CountAll()
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        return await db.BanSyncRecords.LongCountAsync();
    }

    private static IQueryable<BanSyncRecordModel> ApplyOptions(
        XeniaDbContext db,
        QueryOptions options)
        => ApplyOptions(db.BanSyncRecords, options);
    private static IQueryable<BanSyncRecordModel> ApplyOptions(
        IQueryable<BanSyncRecordModel> db,
        QueryOptions options)
    {
        IQueryable<BanSyncRecordModel> q = db;
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
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var q = ApplyOptions(db, options ?? new());
        if (paginationOptions != null) q = q.ApplyPagination(paginationOptions);
        return await q.ToListAsync();
    }

    public async Task<long> CountForGuild(ulong guildId, bool includeGhostedRecords = false)
    {
        var guildIdStr = guildId.ToString();
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var q = db.BanSyncRecords
            .Where(e => e.GuildId == guildIdStr);
        if (!includeGhostedRecords)
        {
            q = q.Where(e => !e.Ghost);
        }
        return await q.LongCountAsync();
    }

    #region Get Info
    public async Task<ICollection<BanSyncRecordModel>> GetInfoEnumerable(
        ulong userId, QueryOptions? options = null,
        PaginationOptions? paginationOptions = null)
    {
        var userIdStr = userId.ToString();
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var q = ApplyOptions(db, options ?? new())
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
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var q = ApplyOptions(db, options ?? new())
            .Where(e => e.UserId == userIdStr && e.GuildId == guildIdStr);
        if (paginationOptions != null)
            q = q.ApplyPagination(paginationOptions);
        return await q.ToListAsync();
    }

    public async Task<BanSyncRecordModel?> GetInfo(ulong userId, ulong guildId, QueryOptions? options = null)
    {
        var userIdStr = userId.ToString();
        var guildIdStr = guildId.ToString();
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        return await ApplyOptions(db, options ?? new())
            .Where(e => e.UserId == userIdStr && e.GuildId == guildIdStr)
            .FirstOrDefaultAsync();
    }
    public async Task<BanSyncRecordModel?> GetInfo(BanSyncRecordModel model, QueryOptions options)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        return await ApplyOptions(db, options)
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
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        return await ApplyOptions(db, options ?? new())
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
        using var db = _dbContextFactory.CreateDbContext();
        var q = ApplyOptions(db, options ?? new())
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
    #endregion
    
    #region Insert or Update
    public async Task InsertOrUpdate(BanSyncRecordModel model)
    {
        if (model.GetGuildId() <= 0)
            throw new ArgumentException($"Invalid value {model.GuildId}", $"{nameof(model)}.{nameof(model.GuildId)}");
        if (model.GetUserId() <= 0)
            throw new ArgumentException($"Invalid value {model.UserId}", $"{nameof(model)}.{nameof(model.UserId)}");

        await using var db = await _dbContextFactory.CreateDbContextAsync();
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
    #endregion
    
    public async Task<bool> Exists(Guid id)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        return await db.BanSyncRecords.AnyAsync(e => e.Id == id);
    }
    public async Task SetGhostState(Guid id, bool state)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        await db.BanSyncRecords.Where(e => e.Id == id)
            .ExecuteUpdateAsync(e => e
            .SetProperty(p => p.Ghost, state));
        await db.SaveChangesAsync();
    }

    #region Mutual Records
    public async Task<ICollection<BanSyncRecordModel>> MutualRecords(
        ulong guildId,
        PaginationOptions paginationOptions,
        QueryOptions? queryOptions = null)
    {
        // check is already done in SP
        if (queryOptions != null)
        {
            queryOptions.IgnoreDisabledGuilds = false;
            queryOptions.IncludeGhostedRecords = true;
        }
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        return await ApplyOptions(
                db.spBanSyncGetMutualRecordsForGuild_Paginate(
                guildId.ToString(),
                paginationOptions.Page - 1,
                paginationOptions.PageSize),
                queryOptions ?? new())
            .AsNoTracking()
            .ToListAsync();
    }
    public async Task<long> MutualRecordsCount(
        ulong guildId,
        QueryOptions? queryOptions = null)
    {
        // check is already done in SP
        if (queryOptions != null)
        {
            queryOptions.IgnoreDisabledGuilds = false;
            queryOptions.IncludeGhostedRecords = true;
        }
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        return await ApplyOptions(
                db.spBanSyncGetMutualRecordsForGuild(guildId.ToString()),
                queryOptions ?? new())
            .AsNoTracking()
            .LongCountAsync();
    }
    #endregion

    #region Search Query
    
    public async Task<IReadOnlyCollection<BanSyncRecordModel>> SearchQuery(
        XeniaDbContext db,
        SearchQueryOptions options)
    {
        return await ApplySearchQueryPagination(
            ApplySearchQuerySort(SearchQueryInternal(db, options), options),
            options)
            .ToArrayAsync();
    }

    public async Task<long> SearchQueryTotalCount(
        XeniaDbContext db,
        SearchQueryOptions options)
    {
        return await SearchQueryInternal(db, options).LongCountAsync();
    }
    
    private static IQueryable<BanSyncRecordModel> SearchQueryInternal(
        XeniaDbContext db,
        SearchQueryOptions options)
    {
        IQueryable<BanSyncRecordModel> rs = db.BanSyncRecords
            .Include(e => e.UserPartialSnapshot)
            .Include(e => e.BanSyncGuild)
            .Include(e => e.CachedGuildMember);
        
        if (options.FilterUserIds.HasValue)
        {
            var strArray =  options.FilterUserIds.Value
                .Distinct()
                .Select(e => e.ToString("D", CultureInfo.InvariantCulture))
                .ToArray();
            rs = rs.Where(e => ((IEnumerable<string>)strArray).Contains(e.UserId));
        }
        if (options.FilterCreatedByUserIds.HasValue)
        {
            var strArray =  options.FilterCreatedByUserIds.Value
                .Distinct()
                .Select(e => e.ToString("D", CultureInfo.InvariantCulture))
                .ToArray();
            rs = rs.Where(e => ((IEnumerable<string>)strArray).Contains(e.BannedByUserId));
        }
        if (options.FilterGuildIds.HasValue)
        {
            var strArray =  options.FilterGuildIds.Value
                .Distinct()
                .Select(e => e.ToString("D", CultureInfo.InvariantCulture))
                .ToArray();
            rs = rs.Where(e => ((IEnumerable<string>)strArray).Contains(e.GuildId));
        }
        if (options.FilterCreatedAtBefore.HasValue)
        {
            var dtValue = options.FilterCreatedAtBefore.Value;
            rs = rs.Where(e => e.CreatedAt <= dtValue);
        }
        if (options.FilterCreatedAtAfter.HasValue)
        {
            var dtValue = options.FilterCreatedAtAfter.Value;
            rs = rs.Where(e => e.CreatedAt >= dtValue);
        }
        if (options.GhostState.HasValue)
        {
            var value = options.GhostState.Value;
            rs = rs.Where(e => e.Ghost == value);
        }

        return rs;
    }

    private static IQueryable<BanSyncRecordModel> ApplySearchQueryPagination(
        IOrderedQueryable<BanSyncRecordModel> rs,
        SearchQueryOptions options)
    {
        var limit = Math.Min(1, options.PageSize);
        var skip = options.PageIndex * limit;
        return rs.Skip(skip).Take(limit);
    }

    private static IOrderedQueryable<BanSyncRecordModel> ApplySearchQuerySort(
        IQueryable<BanSyncRecordModel> rs,
        SearchQueryOptions options)
    {
        return options.SortBy switch
        {
            SearchQuerySortBy.CreatedAt => options.SortDirection == SearchQuerySortDirection.Ascending
                ? rs.OrderBy(e => e.CreatedAt)
                : rs.OrderByDescending(e => e.CreatedAt),
            SearchQuerySortBy.GuildId => options.SortDirection == SearchQuerySortDirection.Ascending
                ? rs.OrderBy(e => e.GuildId)
                : rs.OrderByDescending(e => e.GuildId),
            SearchQuerySortBy.UserId => options.SortDirection == SearchQuerySortDirection.Ascending
                ? rs.OrderBy(e => e.UserId)
                : rs.OrderByDescending(e => e.UserId),
            SearchQuerySortBy.Username => options.SortDirection == SearchQuerySortDirection.Ascending
                ? rs.OrderBy(e => e.UserPartialSnapshot.Username)
                : rs.OrderByDescending(e => e.UserPartialSnapshot.Username),
            _ => options.SortDirection == SearchQuerySortDirection.Ascending
                ? rs.OrderBy(e => e.CreatedAt)
                : rs.OrderByDescending(e => e.CreatedAt)
        };
    }

    public class SearchQueryOptions
    {
        public Maybe<ulong[]> FilterUserIds { get; set; } = Maybe.None;
        public Maybe<ulong[]> FilterCreatedByUserIds { get; set; } = Maybe.None;
        public Maybe<ulong[]> FilterGuildIds { get; set; } = Maybe.None;
        public Maybe<DateTime> FilterCreatedAtBefore { get; init; } = Maybe.None;
        public Maybe<DateTime> FilterCreatedAtAfter { get; init; } = Maybe.None;
        
        public SearchQuerySortBy SortBy { get; init; } = SearchQuerySortBy.CreatedAt;
        public SearchQuerySortDirection SortDirection { get; init; } = SearchQuerySortDirection.Ascending;
        
        /// <summary>
        /// null = don't check
        /// true = Ghost must be True
        /// false = Ghost must be False
        /// </summary>
        public bool? GhostState { get; set; }

        /// <summary>
        /// 0-based
        /// </summary>
        public int PageIndex
        {
            get;
            set => field = Math.Min(0, value);
        }

        public int PageSize
        {
            get;
            set => field = Math.Min(1, value);
        }
    }
    public enum SearchQuerySortBy
    {
        CreatedAt,
        GuildId,
        UserId,
        Username
    }

    public enum SearchQuerySortDirection
    {
        Ascending,
        Descending
    }
    #endregion

    public class QueryOptions
    {
        public bool IncludeUserPartialSnapshot { get; set; } = true;
        public bool IncludeBanSyncGuild { get; set; } = false;
        public bool IncludeGhostedRecords { get; set; } = false;
        public bool IgnoreDisabledGuilds { get; set; } = true;
    }
}
#pragma warning restore CA1822
