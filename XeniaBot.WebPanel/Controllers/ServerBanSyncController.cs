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

}