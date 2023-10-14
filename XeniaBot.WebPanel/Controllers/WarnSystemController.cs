using Microsoft.AspNetCore.Mvc;
using XeniaBot.Data.Controllers.BotAdditions;
using XeniaBot.WebPanel.Helpers;
using XeniaBot.WebPanel.Models;

namespace XeniaBot.WebPanel.Controllers;

[Controller]
public class WarnSystemController: BaseXeniaController
{
    private readonly ILogger<ServerController> _logger;

    public WarnSystemController(ILogger<ServerController> logger)
        : base()
    {
        _logger = logger;
    }

    public async Task<WarnGuildDetailsViewModel> GetDetails(ulong serverId)
    {
        var data = new WarnGuildDetailsViewModel();
        var guild = _discord.GetGuild(serverId);
        data.User = guild.GetUser(AspHelper.GetUserId(HttpContext) ?? 0);
        
        await AspHelper.FillServerModel(serverId, data);
        
        return data;
    }
    
    [HttpGet("~/Warn/Guild/{id}")]
    public async Task<IActionResult> GuildWarns(ulong id, string? messageType = null, string? message = null)
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
        data.User = guildUser;

        await PopulateModel(data);
        if (messageType != null)
            data.MessageType = messageType;
        if (message != null)
            data.Message = message;

        return View("Details", data);

    }
    
}