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
    
    [HttpGet("~/Warn/Info/{id}")]
    public async Task<IActionResult> WarnInfo(string id, string? messageType = null, string? message = null)
    {
        var controller = Program.Core.GetRequiredService<GuildWarnItemConfigController>();
        var warnData = await controller.GetItemsById(id);
        if (warnData == null)
            return View("NotFound");

        var latestWarnData = warnData.FirstOrDefault();
        if (!CanAccess(latestWarnData?.GuildId ?? 0))
            return View("NotAuthorized");
        
        var userId = AspHelper.GetUserId(HttpContext);
        if (userId == null)
            return View("NotFound", "User not found");
        var guild = _discord.GetGuild(latestWarnData?.GuildId ?? 0);
        if (guild == null)
            return View("NotFound", "Guild not found");
        
        var data = new WarnInfoViewModel()
        {
            Guild = guild,
            WarnItem = latestWarnData,
            History = warnData
        };
        await PopulateModel(data);
        if (messageType != null)
            data.MessageType = messageType;
        if (message != null)
            data.Message = message;

        return View("Info", data);
    }
}