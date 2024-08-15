using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XeniaBot.Data.Repositories;
using XeniaBot.Shared;
using XeniaBot.WebPanel.Helpers;
using XeniaBot.WebPanel.Models;
using XeniaBot.WebPanel.Models.Component;

namespace XeniaBot.WebPanel.Controllers;

[Controller]
public class ServerBanSyncController : BaseXeniaController
{
    private readonly ILogger<ServerController> _logger;
    public ServerBanSyncController(ILogger<ServerController> logger)
        : base()
    {
        _logger = logger;
    }

    protected async Task<ServerBanSyncViewModel> GetDetails(ulong guildId, ulong? targetUserId = null)
    {
        var data = new ServerBanSyncViewModel();
        data.Guild = _discord.GetGuild(guildId);
        data.User = data.Guild.GetUser(AspHelper.GetUserId(HttpContext) ?? 0);
        await AspHelper.FillServerModel(guildId, data, targetUserId, HttpContext);
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
        await AspHelper.FillServerModel(guildId, data, cursor, targetUserId, HttpContext);
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
            return PartialView("NotFound", "Guild not found");

        // await PopulateModel(data);
        //
        // if (!AspHelper.IsCurrentUserAdmin(this.HttpContext))
        // {
        //     data.BanSyncRecords = data.BanSyncRecords.Where(v => !v.Ghost).ToList();
        // }
        // var c = data.BanSyncRecords
        //     .OrderByDescending(v => v.Timestamp)
        //     .Skip((cursor - 1) * ServerBanSyncViewModel.PageSize)
        //     .Take(ServerBanSyncViewModel.PageSize)
        //     .ToList();
        // data.BanSyncRecords = c;
        // data.Cursor = cursor;

        var data = await GetComponentDetails(id, cursor, targetUserId);
        await PopulateModel(data);
        data.Cursor = cursor;

        if (!data.BanSyncGuild.Enable)
        {
            return PartialView("NotAuthorized", new NotAuthorizedViewModel()
            {
                Message = "BanSync is not enabled on your server. <a href=\"https://xenia.kate.pet/guide/about_bansync\">More Information</a>"
            });
        }

        return PartialView("IndexComponent", data);
    }

    [HttpGet("~/BanSync/Record/{id}/Ghost/True")]
    [AuthRequired]
    [RequireSuperuser]
    public async Task<IActionResult> GhostEnable(string id)
    {
        var banSyncInfoController = Program.Core.Services.GetRequiredService<BanSyncInfoRepository>();
        try
        {
            var record = await banSyncInfoController.GetInfo(id, true);
            if (record == null)
            {
                return PartialView("NotFound");
            }

            record.Ghost = true;
            await banSyncInfoController.SetInfo(record);
            
            var data = new BanSyncRecordViewModel()
            {
                Record = record
            };
            await PopulateModel(data);

            return PartialView("DetailsComponent", data);
        }
        catch (Exception ex)
        {
            return PartialView("Error");
        }
    }
    [HttpGet("~/BanSync/Record/{id}/Ghost/False")]
    [AuthRequired]
    [RequireSuperuser]
    public async Task<IActionResult> GhostDisable(string id)
    {
        var banSyncInfoController = Program.Core.Services.GetRequiredService<BanSyncInfoRepository>();
        try
        {
            var record = await banSyncInfoController.GetInfo(id, true);
            if (record == null)
            {
                return PartialView("NotFound");
            }

            record.Ghost = true;
            await banSyncInfoController.SetInfo(record);
            
            var data = new BanSyncRecordViewModel()
            {
                Record = record
            };
            await PopulateModel(data);

            return PartialView("DetailsComponent", data);
        }
        catch (Exception ex)
        {
            return PartialView("Error");
        }
    }
    
    [HttpGet("~/BanSync/Record/{id}")]
    [AuthRequired]
    public async Task<IActionResult> RecordInfo(string id)
    {
        var banSyncInfoController = Program.Core.Services.GetRequiredService<BanSyncInfoRepository>();
        var record = await banSyncInfoController.GetInfo(id, true);
        if (record == null)
        {
            return View("NotFound");
        }

        if (!AspHelper.IsCurrentUserAdmin(this.HttpContext) && record.Ghost)
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