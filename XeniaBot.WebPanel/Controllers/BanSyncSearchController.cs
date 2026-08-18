using Discord.WebSocket;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using XeniaBot.Shared.Helpers;
using XeniaBot.WebPanel.Helpers;
using XeniaBot.WebPanel.Models;
using XeniaBot.WebPanel.Models.BanSync;
using XeniaBot.WebPanel.Models.BanSyncSearch;
using XeniaDiscord.Common.Services;
using XeniaDiscord.Data;
using XeniaDiscord.Data.Models.BanSync;
using XeniaDiscord.Data.Repositories;
// ReSharper disable AsyncMethodWithoutAwait

namespace XeniaBot.WebPanel.Controllers;

[Controller]
[Route("~/BanSync/Search")]
public class BanSyncSearchController : BaseXeniaController
{
    private readonly IDbContextFactory<XeniaDbContext> _dbContextFactory;
    private readonly BanSyncGuildRepository _bansyncGuildRepo;
    private readonly BanSyncRecordRepository _bansyncRecordRepo;
    private readonly GuildCacheService _guildCacheService;
    private readonly UserCacheService _userCacheService;
    public BanSyncSearchController(IServiceProvider services)
    {
        _dbContextFactory = services.GetRequiredService<IDbContextFactory<XeniaDbContext>>();
        _bansyncGuildRepo = services.GetRequiredService<BanSyncGuildRepository>();
        _bansyncRecordRepo = services.GetRequiredService<BanSyncRecordRepository>();
        _guildCacheService = services.GetRequiredService<GuildCacheService>();
        _userCacheService = services.GetRequiredService<UserCacheService>();
    }

    [HttpGet("MutualRecords/{guildId}")]
    [HttpPost("MutualRecords/{guildId}")]
    [AuthRequired]
    [RestrictToGuild(GuildIdRouteKey = "guildId")]
    public async Task<IActionResult> MutualRecords(
        ulong guildId,
        [FromQuery]
        int page = 1)
    {
        SocketGuild? guild = null;
        try
        {
            guild = ExceptionHelper.RetryOnTimedOut(() => _discord.GetGuild(guildId));
        }
        // ReSharper disable once EmptyGeneralCatchClause
        catch { }

        var guildModel = await _bansyncGuildRepo.GetAsync(guildId);
        if (guildModel?.Enable != true || guildModel.State != BanSyncGuildState.Active)
        {
            return View("BanSyncNotEnabled", new BanSyncNotEnabledModel
            {
                GuildId = guildId,
                Guild = guild
            });
        }

        var component = await GetMutualRecordsModel(
            guildId,
            new()
            {
                Page = page
            });
        var thisGuildCount = await _bansyncRecordRepo.CountForGuild(
            guildId, 
            AspHelper.IsCurrentUserAdmin(HttpContext));
        var otherGuildCount = await _bansyncRecordRepo.MutualRecordsCount(
            guildId,
            new BanSyncRecordRepository.QueryOptions
            {
                IncludeGhostedRecords = AspHelper.IsCurrentUserAdmin(HttpContext),
                IncludeBanSyncGuild = true,
                IncludeUserPartialSnapshot = true
            }) - thisGuildCount;
        var model = new MutualRecordsListModel
        {
            GuildId = guildId,
            GuildName = guild?.Name ?? guildId.ToString(),
            GuildIconUrl = guild?.IconUrl,
            MemberCount = guild?.MemberCount,
            Component = component,
            ThisServerRecordCount = thisGuildCount,
            OtherServerRecordCount = otherGuildCount
        };

        return View("MutualRecordsList", model);
    }

    [AuthRequired]
    [RestrictToGuild(GuildIdRouteKey = "guildId")]
    [HttpGet("MutualRecords/{guildId}/PageComponent")]
    public async Task<IActionResult> MutualRecordsComponent(
        ulong guildId,
        [FromQuery]
        int page = 1)
    {
        var guild = ExceptionHelper.RetryOnTimedOut(() => _discord.GetGuild(guildId));
        if (guild == null)
        {
            return PartialView("NotFoundPartial", $"Guild not found: {guildId}");
        }

        var guildModel = await _bansyncGuildRepo.GetAsync(guild.Id);
        if (guildModel?.Enable != true || guildModel.State != BanSyncGuildState.Active)
        {
            return PartialView("_PartialBanSyncNotEnabled", new BanSyncNotEnabledModel
            {
                GuildId = guildId,
                Guild = guild
            });
        }

        var model = await GetMutualRecordsModel(guildId, new()
        {
            Page = page
        });
        return PartialView("MutualRecordsListSection", model);
    }
    
    private async Task<MutualRecordsListComponentModel> GetMutualRecordsModel(
        ulong guildId,
        BanSyncMutualRecordsQuery query)
    {
        var recordsOpts = new BanSyncRecordRepository.QueryOptions
        {
            IncludeGhostedRecords = AspHelper.IsCurrentUserAdmin(HttpContext),
            IncludeBanSyncGuild = true,
            IncludeUserPartialSnapshot = true
        };
        var records = await _bansyncRecordRepo.MutualRecords(
            guildId,
            new()
            {
                Page = query.Page,
                PageSize = MaxPageSize
            }, recordsOpts);


        var currentGuildCount = await _bansyncRecordRepo.CountForGuild(guildId, recordsOpts.IncludeGhostedRecords);
        var totalCount = await _bansyncRecordRepo.MutualRecordsCount(guildId, recordsOpts);
        var model = new MutualRecordsListComponentModel
        {
            Items = records,
            Page = query.Page,
            PageSize = MaxPageSize,
            GuildId = guildId,
            
            CurrentGuildCount = currentGuildCount,
            TotalCount = totalCount,
            OtherGuildCount = totalCount - currentGuildCount
        };

        await using var db = await _dbContextFactory.CreateDbContextAsync();
        await using var trans = await db.Database.BeginTransactionAsync();
        try
        {
            var guildIconDict = new Dictionary<ulong, string?>();
            var userImageDict = new Dictionary<ulong, string?>();
            foreach (var i in records.Select(e => e.GetGuildId()).Distinct())
            {
                guildIconDict[i] = await _guildCacheService.GetIconUrl(db, i, saveChanges: false);
            }
            foreach (var i in records.Select(e => e.GetUserId()).Distinct())
            {
                userImageDict[i] = await _userCacheService.GetDisplayAvatarUrl(db, i, saveChanges: false);
            }
            model.UserIdProfileDict = userImageDict;
            model.GuildIdProfileDict = guildIconDict;
            await db.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }

        return model;
    }

    public const int MaxPageSize = 25;

    [HttpPost("PerformSearch")]
    public async Task<IActionResult> PerformSearch()
    {
        throw new NotImplementedException();
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
                    CreatedAt = new DateTimeOffset(record.CreatedAt, TimeSpan.Zero).ToUnixTimeSeconds(),
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
            RequestedForGuildId = query.RequestingGuildId.HasValue
                ? query.RequestingGuildId.Value : null,
            RequestedForUserId = AspHelper.GetUserId(HttpContext),
            TotalRecordCount = count,
            Records = resultRecords?.ToArray() ?? [],
            RequestQuery = query.ToQueryDto(),
        };
    }
}
