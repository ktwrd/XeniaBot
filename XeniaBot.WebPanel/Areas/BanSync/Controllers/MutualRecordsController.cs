using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Shared.Helpers;
using XeniaBot.WebPanel.Areas.BanSync.Models.MutualRecords;
using XeniaBot.WebPanel.Areas.BanSync.Models.Search;
using XeniaBot.WebPanel.Controllers;
using XeniaBot.WebPanel.Helpers;
using XeniaBot.WebPanel.Models.BanSync;
using XeniaDiscord.Common.Services;
using XeniaDiscord.Data;
using XeniaDiscord.Data.Models.BanSync;
using XeniaDiscord.Data.Repositories;

namespace XeniaBot.WebPanel.Areas.BanSync.Controllers;

[Controller]
[Area("BanSync")]
[Route("~/BanSync/MutualRecords")]
public class MutualRecordsController : BaseXeniaController
{
    private readonly IDbContextFactory<XeniaDbContext> _dbContextFactory;
    private readonly BanSyncGuildRepository _bansyncGuildRepo;
    private readonly BanSyncRecordRepository _bansyncRecordRepo;
    private readonly GuildCacheService _guildCacheService;
    private readonly UserCacheService _userCacheService;
    public MutualRecordsController(IServiceProvider services)
    {
        _dbContextFactory = services.GetRequiredService<IDbContextFactory<XeniaDbContext>>();
        _bansyncGuildRepo = services.GetRequiredService<BanSyncGuildRepository>();
        _bansyncRecordRepo = services.GetRequiredService<BanSyncRecordRepository>();
        _guildCacheService = services.GetRequiredService<GuildCacheService>();
        _userCacheService = services.GetRequiredService<UserCacheService>();
    }
    
    [HttpGet("{guildId}")]
    [HttpPost("{guildId}")]
    [AuthRequired]
    [RestrictToGuild(GuildIdRouteKey = "guildId", RequiredPermission = GuildPermission.ModerateMembers)]
    public async Task<IActionResult> MutualRecords(
        ulong guildId,
        [FromQuery]
        int page = 1,
        [FromQuery(Name = "forUser")]
        ulong? forUser = null)
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
            new BanSyncMutualRecordsQuery
            {
                Page = page,
                ForUserId = forUser,
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

        return View("List", model);
    }

    [AuthRequired]
    [RestrictToGuild(GuildIdRouteKey = "guildId")]
    [HttpGet("{guildId}/PageComponent")]
    public async Task<IActionResult> MutualRecordsComponent(
        ulong guildId,
        [FromQuery]
        int page = 1,
        [FromQuery(Name = "forUser")]
        ulong? forUser = null)
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

        var model = await GetMutualRecordsModel(
            guildId,
            new BanSyncMutualRecordsQuery
            {
                Page = page,
                ForUserId = forUser,
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
        var paginationOptions = new PaginationOptions
        {
            Page = query.Page,
            PageSize = MaxPageSize
        };
        var records = query.ForUserId.HasValue
            ? await _bansyncRecordRepo.MutualRecords(guildId, query.ForUserId.Value, paginationOptions, recordsOpts)
            : await _bansyncRecordRepo.MutualRecords(guildId, paginationOptions, recordsOpts);


        var currentGuildCount = await _bansyncRecordRepo.CountForGuild(guildId, recordsOpts.IncludeGhostedRecords);
        var totalCount = query.ForUserId.HasValue
            ? await _bansyncRecordRepo.MutualRecordsCount(guildId, query.ForUserId.Value, recordsOpts)
            : await _bansyncRecordRepo.MutualRecordsCount(guildId, recordsOpts);
        var model = new MutualRecordsListComponentModel
        {
            Items = records,
            Page = query.Page,
            PageSize = MaxPageSize,
            GuildId = guildId,
            ForUserId = query.ForUserId,
            
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
}