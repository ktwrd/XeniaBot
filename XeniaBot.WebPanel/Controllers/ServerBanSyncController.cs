using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XeniaBot.WebPanel.Helpers;
using XeniaBot.WebPanel.Models;
using XeniaBot.WebPanel.Models.Component;
using XeniaDiscord.Data.Repositories;

namespace XeniaBot.WebPanel.Controllers;

[Controller]
public class ServerBanSyncController : BaseXeniaController
{
    private readonly ILogger<ServerBanSyncController> _logger;
    private readonly BanSyncRecordRepository _bansyncRecordRepository;
    public ServerBanSyncController(
        IServiceProvider services,
        ILogger<ServerBanSyncController> logger)
        : base()
    {
        _bansyncRecordRepository = services.GetRequiredService<BanSyncRecordRepository>();
        _logger = logger;
    }

    protected async Task<ServerBanSyncViewModel> GetDetails(ulong guildId, ulong? targetUserId = null)
    {
        var data = new ServerBanSyncViewModel();
        data.Guild = _discord.GetGuild(guildId);
        data.User = data.Guild.GetUser(AspHelper.GetUserId(HttpContext) ?? 0);
        // await AspHelper.FillServerModel(guildId, data, targetUserId, HttpContext);
        // TODO see method below with commented out methid
        return data;
    }

    protected async Task<BanSyncMutualRecordsListComponentViewModel> GetComponentDetails(ulong guildId,
        int cursor,
        ulong? targetUserId)
    {
        var data = new BanSyncMutualRecordsListComponentViewModel();
        var g = _discord.GetGuild(guildId);
        data.Guild = g;
        data.User = g.GetUser(AspHelper.GetUserId(HttpContext) ?? 0);
        // TODO fix
        // await AspHelper.FillServerModel(guildId, data, cursor, targetUserId, HttpContext);
        /* ^ this was:
        public static async Task FillServerModel(ulong serverId, IBanSyncBaseRecords data, ulong? targetUserId, HttpContext context)
        {
            data.FilterRecordsByUserId = targetUserId;
            var banSyncRecordConfig = Program.Core.GetRequiredService<BanSyncInfoRepository>();
            data.BanSyncRecordCount = await banSyncRecordConfig.GetInfoAllInGuildCount(
                serverId,
                targetUserId,
                allowGhost: AspHelper.IsCurrentUserAdmin(context));

            var banSyncGuildConfig = Program.Core.GetRequiredService<BanSyncStateHistoryRepository>();
            data.BanSyncGuild = await banSyncGuildConfig.GetLatest(serverId) ?? new BanSyncStateHistoryItemModel()
            {
                GuildId = serverId
            };
        }*/
        data.Cursor = cursor;
        return data;
    }

    [HttpGet("~/Server/{id}/BanSync")]
    [AuthRequired]
    [RestrictToGuild(GuildIdRouteKey = "id")]
    public async Task<IActionResult> Index(ulong id, string? messageType = null, string? message = null, ulong? targetUserId = null)
    {
        var guild = _discord.GetGuild(id);
        if (guild == null)
            return View("NotFound", "Guild not found");

        var data = await GetDetails(guild.Id, targetUserId);
        await PopulateModel(data);

        if (messageType != null)
            data.MessageType = messageType;
        if (message != null)
            data.Message = message;

        if (!data.BanSyncGuild.Enable)
        {
            return View("NotAuthorized", new NotAuthorizedViewModel()
            {
                Message = "BanSync is not enabled on your server. <a href=\"https://xenia.kate.pet/guide/about_bansync\">More Information</a>"
            });
        }

        return View("Index", data);
    }

    [HttpGet("~/Server/{id}/BanSync/ListComponent")]
    [AuthRequired]
    [RestrictToGuild(GuildIdRouteKey = "id")]
    public async Task<IActionResult> ListComponent(ulong id, int cursor = 1, ulong? targetUserId = null)
    {
        var guild = _discord.GetGuild(id);
        if (guild == null)
            return PartialView("NotFoundComponent", "Guild not found");

        var data = await GetComponentDetails(id, cursor, targetUserId);
        await PopulateModel(data);
        data.Cursor = cursor;

        if (!data.BanSyncGuild.Enable)
        {
            return PartialView("NotAuthorizedComponent", new NotAuthorizedViewModel()
            {
                Message = "BanSync is not enabled on your server. <a href=\"https://xenia.kate.pet/guide/about_bansync\">More Information</a>"
            });
        }

        return PartialView("IndexComponent", data);
    }

    [HttpGet("~/BanSync/Record/{id}/Ghost/True")]
    [AuthRequired]
    [RequireSuperuser]
    public async Task<IActionResult> GhostEnable(Guid id)
    {
        try
        {
            var record = await _bansyncRecordRepository.GetInfo(id, new()
            {
                IncludeBanSyncGuild = true,
                IncludeUserPartialSnapshot = true,
                IncludeGhostedRecords = AspHelper.IsCurrentUserAdmin(HttpContext)
            });
            if (record == null)
            {
                return View("NotFound");
            }
            await _bansyncRecordRepository.SetGhostState(id, true);
            record.Ghost = true;
            var data = new BanSyncRecordViewModel()
            {
                Record = record
            };
            await PopulateModel(data);

            return View("DetailsComponent", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set Ghost=True for record {RecordId}", id);
            throw;
        }
    }

    [HttpGet("~/BanSync/Record/{id}/Ghost/False")]
    [AuthRequired]
    [RequireSuperuser]
    public async Task<IActionResult> GhostDisable(Guid id)
    {
        try
        {
            var record = await _bansyncRecordRepository.GetInfo(id, new()
            {
                IncludeBanSyncGuild = true,
                IncludeUserPartialSnapshot = true,
                IncludeGhostedRecords = AspHelper.IsCurrentUserAdmin(HttpContext)
            });
            if (record == null)
            {
                return View("NotFound");
            }
            await _bansyncRecordRepository.SetGhostState(id, false);
            record.Ghost = false;
            var data = new BanSyncRecordViewModel()
            {
                Record = record
            };
            await PopulateModel(data);

            return View("DetailsComponent", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set Ghost=False for record {RecordId}", id);
            throw;
        }
    }

    [HttpGet("~/BanSync/Record/{id}")]
    [AuthRequired]
    public async Task<IActionResult> RecordInfo(string id)
    {
        var record = await _bansyncRecordRepository.GetInfo(id, new()
        {
            IncludeBanSyncGuild = true,
            IncludeUserPartialSnapshot = true,
            IncludeGhostedRecords = AspHelper.IsCurrentUserAdmin(HttpContext)
        });
        if (record == null)
        {
            return View("NotFound");
        }

        var data = new BanSyncRecordViewModel()
        {
            Record = record
        };
        await PopulateModel(data);
        return View("Details", data);
    }
}