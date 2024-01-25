using Microsoft.AspNetCore.Mvc;
using XeniaBot.Data.Controllers.BotAdditions;
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
    public async Task<IActionResult> Index(ulong id, string? messageType = null, string? message = null)
    {
        if (!CanAccess(id))
            return View("NotAuthorized");
        
        var userId = AspHelper.GetUserId(HttpContext);
        if (userId == null)
            return View("NotFound", "User not found");
        var user = _discord.GetUser((ulong)userId);
        var guild = _discord.GetGuild(id);
        if (guild == null)
            return View("NotFound", "Guild not found");
        var guildUser = guild.GetUser(user.Id);

        var data = await GetDetails(guild.Id);
        await PopulateModel(data);
        if (messageType != null)
            data.MessageType = messageType;
        if (message != null)
            data.Message = message;

        return View("Index", data);
    }

    [HttpGet("~/BanSync/Record/{id}")]
    public async Task<IActionResult> RecordInfo(string id)
    {
        var idSplit = id.Split("_");
        if (idSplit.Length < 2)
            return View("NotFound");

        ulong targetUserId = 0;
        try
        { targetUserId = ulong.Parse(idSplit[0]);
        } catch
        { return View("NotFound"); }

        ulong targetGuildId = 0;
        try
        { targetGuildId = ulong.Parse(idSplit[1]);
        } catch
        { return View("NotFound"); }

        var banSyncInfoController = Program.Services.GetRequiredService<BanSyncInfoConfigController>();
        try
        {
            var record = await banSyncInfoController.GetInfo(targetUserId, targetGuildId);
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
        catch (Exception ex)
        {
            return View("Error");
        }
    }
}