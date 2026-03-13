using Discord.WebSocket;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using XeniaBot.WebPanel.Helpers;
using XeniaBot.WebPanel.Models.BanSync;
using XeniaBot.WebPanel.Models.BanSyncSearch;
using XeniaDiscord.Data;
using XeniaDiscord.Data.Models.BanSync;
using XeniaDiscord.Data.Repositories;

namespace XeniaBot.WebPanel.Controllers;

[Controller]
[Route("~/BanSync/Search")]
public class BanSyncSearchController : BaseXeniaController
{
    private readonly XeniaDbContext _db;
    private readonly BanSyncGuildRepository _bansyncGuildRepo;
    private readonly BanSyncRecordRepository _bansyncRecordRepo;
    private readonly DiscordSocketClient _discord;
    public BanSyncSearchController(IServiceProvider services)
    {
        _db = services.GetRequiredService<XeniaDbContext>();
        _bansyncGuildRepo = services.GetRequiredService<BanSyncGuildRepository>();
        _bansyncRecordRepo = services.GetRequiredService<BanSyncRecordRepository>();
        _discord = services.GetRequiredService<DiscordSocketClient>();
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
            guild = _discord.GetGuild(guildId);
        }
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

        var component = await GetMutualRecordsModel(guildId, new()
        {
            Page = page
        });
        var model = new MutualRecordsListModel
        {
            GuildId = guildId,
            GuildName = guild?.Name ?? guildId.ToString(),
            GuildIconUrl = guild?.IconUrl,
            MemberCount = guild?.MemberCount,
            Component = component
        };

        model.ThisServerRecordCount = await _bansyncRecordRepo.CountForGuild(guildId, AspHelper.IsCurrentUserAdmin(HttpContext));
        model.OtherServerRecordCount = await _bansyncRecordRepo.MutualRecordsCount(guildId, new BanSyncRecordRepository.QueryOptions()
        {
            IncludeGhostedRecords = AspHelper.IsCurrentUserAdmin(HttpContext),
            IncludeBanSyncGuild = true,
            IncludeUserPartialSnapshot = true
        }) - model.ThisServerRecordCount;

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
        var guild = _discord.GetGuild(guildId);
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

    private async Task<MutualRecordsListComponentModel> GetMutualRecordsModel(ulong guildId, BanSyncMutualRecordsQuery query)
    {
        var recordsOpts = new BanSyncRecordRepository.QueryOptions()
        {
            IncludeGhostedRecords = AspHelper.IsCurrentUserAdmin(HttpContext),
            IncludeBanSyncGuild = true,
            IncludeUserPartialSnapshot = true
        };
        var records = await _bansyncRecordRepo.MutualRecords(guildId,
            new()
            {
                Page = query.Page,
                PageSize = MaxPageSize
            }, recordsOpts);


        var model = new MutualRecordsListComponentModel()
        {
            Items = records,
            Page = query.Page,
            PageSize = MaxPageSize,
            GuildId = guildId
        };

        model.CurrentGuildCount = await _bansyncRecordRepo.CountForGuild(guildId, recordsOpts.IncludeGhostedRecords);
        model.TotalCount = await _bansyncRecordRepo.MutualRecordsCount(guildId, recordsOpts);
        model.OtherGuildCount = model.TotalCount - model.CurrentGuildCount;

        return model;
    }

    public const int MaxPageSize = 10;

    [HttpPost("Perform")]
    public async Task<IActionResult> PerformSearch()
    {
        throw new NotImplementedException();
    }
}
