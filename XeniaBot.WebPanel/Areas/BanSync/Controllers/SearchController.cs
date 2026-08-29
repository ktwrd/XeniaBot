using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using CSharpFunctionalExtensions.Json.Serialization;
using Discord.WebSocket;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XeniaBot.Shared;
using XeniaBot.Shared.Helpers;
using XeniaBot.WebPanel.Areas.BanSync.Models.Search;
using XeniaBot.WebPanel.Controllers;
using XeniaBot.WebPanel.Helpers;
using XeniaBot.WebPanel.Models;
using XeniaDiscord.Common.Services;
using XeniaDiscord.Data;
using XeniaDiscord.Data.Models.BanSync;
using XeniaDiscord.Data.Repositories;
// ReSharper disable AsyncMethodWithoutAwait

namespace XeniaBot.WebPanel.Areas.BanSync.Controllers;

[Controller]
[Route("~/BanSync/Search")]
[Area("BanSync")]
public class SearchController : BaseXeniaController
{
    private readonly IDbContextFactory<XeniaDbContext> _dbContextFactory;
    private readonly BanSyncGuildRepository _bansyncGuildRepo;
    private readonly BanSyncRecordRepository _bansyncRecordRepo;
    private readonly GuildCacheService _guildCacheService;
    private readonly UserCacheService _userCacheService;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<SearchController> _logger;
    public SearchController(IServiceProvider services, ILogger<SearchController> logger)
    {
        _logger = logger;
        _dbContextFactory = services.GetRequiredService<IDbContextFactory<XeniaDbContext>>();
        _bansyncGuildRepo = services.GetRequiredService<BanSyncGuildRepository>();
        _bansyncRecordRepo = services.GetRequiredService<BanSyncRecordRepository>();
        _guildCacheService = services.GetRequiredService<GuildCacheService>();
        _userCacheService = services.GetRequiredService<UserCacheService>();
        _memoryCache = services.GetRequiredService<IMemoryCache>();
    }
    
    [HttpGet]
    public async Task<IActionResult> PerformSearchGet()
    {
        return await PerformSearchForm(new BanSyncSearchQuery
        {
            RequestingGuildId = 1,
        });
    }
    
    [HttpPost]
    public async Task<IActionResult> PerformSearchForm(
        BanSyncSearchQuery formQuery)
    {
        var isRequestorAdmin = AspHelper.IsCurrentUserAdmin(HttpContext);
        var query = new BanSyncSearchApiQuery()
        {
            FilterUserIds = formQuery.FilterUserIds == null
                ? Maybe.None
                : formQuery.FilterUserIds.Distinct().ToArray(),
            FilterCreatedByUserIds = formQuery.FilterCreatedByUserIds == null
                ? Maybe.None
                : formQuery.FilterCreatedByUserIds.Distinct().ToArray(),
            FilterGuildIds = formQuery.GuildIdFilter == null
                ? Maybe.None
                : formQuery.GuildIdFilter.Distinct().ToArray(),
            SortBy = formQuery.SortBy ?? BanSyncSearchApiQuerySortBy.CreatedAt,
            SortDirection = formQuery.SortDirection ?? BanSyncSearchApiQuerySortDirection.Descending,
            RequestingGuildId = formQuery.RequestingGuildId <= 1 ? Maybe.None : formQuery.RequestingGuildId,
            EnforceGuildVisibility = !isRequestorAdmin,
            IncludeRequestingGuildRecordsInResult = formQuery.IncludeRequestingGuild ?? false,
            Pagination = new BanSyncSearchApiQueryPagination
            {
                Page = formQuery.Page,
                Limit = formQuery.PageSize
            },
            GhostState = formQuery.IncludeGhostRecords && isRequestorAdmin
                ? null
                : isRequestorAdmin && formQuery.GhostState
        };
        
        // kick them out if it's not prompting the user to select a guild, or 
        var requestingGuildExists = formQuery.RequestingGuildId > 1
                                    && ExceptionHelper.RetryOnTimedOut(() => _discord.GetGuild(formQuery.RequestingGuildId) != null);

        if (!isRequestorAdmin && !requestingGuildExists && formQuery.RequestingGuildId != 1)
        {
            return View("NotFound", new NotFoundViewModel()
            {
                Message = $"Xenia is not a member of the guild you are performing a search for: {formQuery.RequestingGuildId}",
            });
        }
        
        var resultViewModel = new SearchViewModel
        {
            Query = query,
            IsSysadmin = isRequestorAdmin
        };
        
        var repoQuery = query.ToRepositoryOptions();
        
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        resultViewModel.AvailableGuilds = await GetAccessibleStrippedGuilds(db);

        if (query.EnforceGuildVisibility)
        {
            if (repoQuery.FilterGuildIds.HasValue)
            {
                repoQuery.FilterGuildIds = repoQuery.FilterGuildIds.Value
                    .Where(e => resultViewModel.AvailableGuilds.Any(x => x.Id == e))
                    .ToArray();
            }
            else
            {
                repoQuery.FilterGuildIds = resultViewModel.AvailableGuilds.Select(e => e.Id).ToArray();
            }
        }

        if (formQuery.RequestingGuildId == 1)
        {
            resultViewModel.Alert = new AlertComponentViewModel
            {
                Message = "Please select a Guild in the filter options to run a search on behalf of.",
                ShowClose = false,
                Type = AlertComponentType.Info
            };
        }
        else
        {
            var count = await _bansyncRecordRepo.SearchQueryTotalCount(db, repoQuery);
            if (count > 0)
            {
                resultViewModel.Records = await _bansyncRecordRepo.SearchQuery(db, repoQuery);
            }
            else
            {
                resultViewModel.Alert = new AlertComponentViewModel
                {
                    Message = "No records found.",
                    ShowClose = true,
                    Type = AlertComponentType.Warning
                };
            }
        }

        
        return View("Default", resultViewModel);
    }
    
    private async Task<IReadOnlyCollection<ulong>> GetAccessibleGuildIdsInternal(
        XeniaDbContext db)
    {
        var currentUserId = AspHelper.GetUserId(HttpContext);
        if (!currentUserId.HasValue) return [];
        
        // get array of target guilds, and only include active guilds when user isn't a bot dev.
        var targetGuilds = db.BanSyncGuilds
            .AsNoTracking()
            .Select(e => new { e.GuildId, e.State})
            .Distinct()
            .ToArray();
        // var isAdmin = AspHelper.IsCurrentUserAdmin(HttpContext);
        var isAdmin = false;
        if (!isAdmin)
            targetGuilds = [.. targetGuilds.Where(e => e.State == BanSyncGuildState.Active)];
        var activeGuildIds = targetGuilds
            .Select(e => ulong.TryParse(e.GuildId, out var u) ? u : 0)
            .ToArray();
        if (isAdmin)
            return activeGuildIds;
        
        var inGuilds = _discord.Guilds
            .Where(e => activeGuildIds.Contains(e.Id))
            .Where(e => e.Users.Any(u => u.Id == currentUserId.Value));
        var canAccessGuilds = new HashSet<ulong>();
        foreach (var guild in inGuilds)
        {
            SocketGuildUser? member;
            try
            {
                member = ExceptionHelper.RetryOnTimedOut(() => guild.GetUser(currentUserId.Value));
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Failed to get member in guild (userId: {currentUserId}, guildId: {guild.Id})", e);
            }
            if (member == null) continue;

            if (member.GuildPermissions.ModerateMembers)
            {
                canAccessGuilds.Add(guild.Id);
            }
        }

        return canAccessGuilds;
    }

    private async Task<IReadOnlyCollection<StrippedGuild>> GetAccessibleStrippedGuilds(
        XeniaDbContext db)
    {
        const string cacheKey = "AccessibleStrippedGuildsForBanSyncSearch.";
        var requestingUser = AspHelper.GetUserId(HttpContext);
        if (requestingUser is null or < 1) return [];
        
        IReadOnlyCollection<StrippedGuild> data;
        var key = cacheKey + requestingUser.Value.ToString("X");
        if (_memoryCache.TryGetValue(cacheKey + requestingUser, out string? cacheValue))
        {
            var parsed = cacheValue == null ? null : JsonSerializer.Deserialize<StrippedGuild[]>(cacheValue, CacheSerializerOptions);
            data = parsed ?? await GetAccessibleStrippedGuildsInternal(db);
            if (parsed == null)
                MemoryWrite();
            _logger.LogTrace("Cache hit: {Key}", key);
        }
        else
        {
            data = await GetAccessibleStrippedGuildsInternal(db);
            MemoryWrite();
            _logger.LogTrace("Cache write: {Key}", key);
        }

        return data;

        void MemoryWrite()
        {
            var str = JsonSerializer.Serialize(data, CacheSerializerOptions);
            _memoryCache.Set(key, str,
                new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(1)));
        }
    }
    
    private async Task<IReadOnlyCollection<StrippedGuild>> GetAccessibleStrippedGuildsInternal(
        XeniaDbContext db)
    {
        var guildIds = await GetAccessibleGuildIdsInternal(db);
        return await GetAccessibleStrippedGuildsInternal(db, guildIds);
    }

    private async Task<IReadOnlyCollection<StrippedGuild>> GetAccessibleStrippedGuildsInternal(
        XeniaDbContext db,
        IReadOnlyCollection<ulong> guildIds)
    {
        var discordGuilds = _discord.Guilds.Where(e => guildIds.Contains(e.Id)).ToArray();
        var idsNotInDiscord = guildIds.Where(e => discordGuilds.All(x => x.Id != e)).ToArray();
        var strIdsNotInDiscord = idsNotInDiscord.Select(e => e.ToString("D")).ToArray();
        var result = new List<StrippedGuild>();
        var visitedGuildIds = new HashSet<ulong>();
        var cachedGuilds = await db.GuildCache
            .AsNoTracking()
            .Where(e => !((IEnumerable<string>)strIdsNotInDiscord).Contains(e.Id))
            .ToListAsync();
        // try and get stripped guilds in this order:
        // - discord
        // - guild cache
        // - guild snapshots
        // - guild partial snapshots
        foreach (var guild in discordGuilds)
        {
            if (visitedGuildIds.Add(guild.Id))
            {
                result.Add(StrippedGuild.FromExisting(guild));
            }
        }
        foreach (var cachedGuild in cachedGuilds)
        {
            ulong id;
            try
            {
                id = cachedGuild.GetGuildId();
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(
                    $"Failed to parse GuildId from CacheGuildModel (value: {cachedGuild.Id})", e);
            }
            if (visitedGuildIds.Add(id))
            {
                result.Add(StrippedGuild.FromExisting(cachedGuild, id));
            }
        }
        if (idsNotInDiscord.Any(x => !visitedGuildIds.Contains(x)))
        {
            foreach (var guildId in idsNotInDiscord)
            {
                var idStr = guildId.ToString("D");
                if (!visitedGuildIds.Add(guildId)) continue;
                var snapshot = await db.GuildSnapshots
                    .AsNoTracking()
                    .Where(e => e.GuildId == idStr)
                    .OrderByDescending(e => e.CreatedAt)
                    .FirstOrDefaultAsync();
                if (snapshot != null)
                {
                    result.Add(StrippedGuild.FromExisting(null, snapshot, guildId));
                    continue;
                }
                var partialSnapshot = await db.GuildPartialSnapshots
                    .AsNoTracking()
                    .Where(e => e.GuildId == idStr)
                    .OrderByDescending(e => e.Timestamp)
                    .FirstOrDefaultAsync();
                result.Add(StrippedGuild.FromExisting(partialSnapshot, guildId));
            }
        }

        // return combined list of real guilds (sort name asc),
        // with guilds at the end not in cache or snapshot or discord (sort id asc)
        return
        [
            .. result.Where(e => e.Name != e.Id.ToString())
                .OrderBy(e => e.Name)
                .Concat(result.Where(e => e.Name == e.Id.ToString()).OrderBy(e => e.Id))
        ];
    }

    [AuthRequired]
    [RestrictToGuild(GuildIdRouteKey = "guildId")]
    [HttpPost("api/v1/PerformSearch")]
    public async Task<IActionResult> PerformSearchApiRoute(
        [FromQuery(Name = "guildId")]
        ulong guildId,
        [FromBody]
        BanSyncSearchApiQueryDtoV1 searchQuery)
    {
        var searchQueryRecord = searchQuery.ToRecord();
        searchQueryRecord.EnforceGuildVisibility = !AspHelper.IsCurrentUserAdmin(HttpContext);
        var guildExists = ExceptionHelper.RetryOnTimedOut(() => _discord.GetGuild(guildId) != null);
        if (searchQueryRecord.EnforceGuildVisibility && !guildExists)
        {
            return Json(new NotFoundApiResponse
            {
                Message = "Guild not found: " + guildId
            });
        }
        
        var data = await GetSearchResults(searchQueryRecord);
        return Json(data);
    }

    [SuppressMessage("ReSharper", "InvertIf")]
    [SuppressMessage("ReSharper", "LoopCanBeConvertedToQuery")]
    [SuppressMessage("ReSharper", "ConditionalAccessQualifierIsNonNullableAccordingToAPIContract")]
    private async Task<BanSyncSearchApiResultV1> GetSearchResults(
        BanSyncSearchApiQuery query)
    {
        var repoQuery = query.ToRepositoryOptions();
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var count = await _bansyncRecordRepo.SearchQueryTotalCount(db, repoQuery);
        List<BanSyncSearchApiResultRecordV1>? resultRecords = null;
        if (count > 0)
        {
            var records = await _bansyncRecordRepo.SearchQuery(db, repoQuery);
            resultRecords = new List<BanSyncSearchApiResultRecordV1>(records.Count);
            foreach (var record in records)
            {
                var item = new BanSyncSearchApiResultRecordV1
                {
                    RecordId = record.Id.ToString("N", CultureInfo.InvariantCulture).ToLowerInvariant(),
                    GuildId = record.GetGuildId(),
                    UserId = record.GetUserId(),
                    GuildName = record.GuildName,
                    Username = record.UserPartialSnapshot?.FormatUsername() ?? string.Empty,
                    CreatedAt = record.CreatedAt.ToUnixTimeSeconds(),
                    CreatedByUserId = record.GetBannedByUserId(),
                    Reason = string.IsNullOrWhiteSpace(record.Reason?.Trim())
                        ? null
                        : record.Reason.Trim()
                };
                resultRecords.Add(item);
            }
        }
        
        return new BanSyncSearchApiResultV1
        {
            RequestedForGuildId = query.RequestingGuildId.ToNullable(),
            RequestedForUserId = AspHelper.GetUserId(HttpContext),
            TotalRecordCount = count,
            Records = resultRecords?.ToArray() ?? [],
            RequestQuery = query.ToQueryDto(),
        };
    }
    
    private static JsonSerializerOptions CacheSerializerOptions
        => new JsonSerializerOptions()
        {
            ReferenceHandler = ReferenceHandler.Preserve
        }.AddCSharpFunctionalExtensionsConverters();
}