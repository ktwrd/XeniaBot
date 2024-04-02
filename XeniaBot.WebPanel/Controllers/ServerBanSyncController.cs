using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XeniaBot.Data.Repositories;
using XeniaBot.WebPanel.Helpers;
using XeniaBot.WebPanel.Models;

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

    protected async Task<ServerBanSyncViewModel> GetDetails(ulong guildId)
    {
        var data = new ServerBanSyncViewModel();
        data.Guild = _discord.GetGuild(guildId);
        data.User = data.Guild.GetUser(AspHelper.GetUserId(HttpContext) ?? 0);
        data = await AspHelper.FillServerModel(guildId, data);
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

        var data = await GetDetails(guild.Id);
        await PopulateModel(data);

        if (!AspHelper.IsCurrentUserAdmin(this.HttpContext))
        {
            data.BanSyncRecords = data.BanSyncRecords.Where(v => !v.Ghost).ToList();
        }
        
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
        
        if (targetUserId != null)
        {
            data.FilterRecordsByUserId = targetUserId;
            data.BanSyncRecords = data.BanSyncRecords.Where(v => v.UserId == targetUserId).ToList();
        }

        return View("Index", data);
    }

    [HttpGet("~/BanSync/Record/{id}/Ghost/True")]
    [AuthRequired]
    [RequireSuperuser]
    public async Task<IActionResult> GhostEnable(string id)
    {
        var banSyncInfoController = Program.Core.Services.GetRequiredService<BanSyncInfoRepository>();
        try
        {
            var record = await banSyncInfoController.GetInfo(id);
            if (record == null)
            {
                return View("NotFound");
            }

            record.Ghost = true;
            await banSyncInfoController.SetInfo(record);
            
            var data = new BanSyncRecordViewModel()
            {
                Record = record
            };
            await PopulateModel(data);

            return View("Details", data);
        }
        catch (Exception ex)
        {
            return View("Error");
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
            var record = await banSyncInfoController.GetInfo(id);
            if (record == null)
            {
                return View("NotFound");
            }

            record.Ghost = true;
            await banSyncInfoController.SetInfo(record);
            
            var data = new BanSyncRecordViewModel()
            {
                Record = record
            };
            await PopulateModel(data);

            return View("Details", data);
        }
        catch (Exception ex)
        {
            return View("Error");
        }
    }
    
    [HttpGet("~/BanSync/Record/{id}")]
    [AuthRequired]
    public async Task<IActionResult> RecordInfo(string id)
    {
        var banSyncInfoController = Program.Core.Services.GetRequiredService<BanSyncInfoRepository>();
        try
        {
            var record = await banSyncInfoController.GetInfo(id);
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
        catch (Exception ex)
        {
            return View("Error");
        }
    }
}