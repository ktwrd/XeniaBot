using Microsoft.AspNetCore.Mvc;
using XeniaBot.Data.Controllers.BotAdditions;
using XeniaBot.Data.Models;
using XeniaBot.Shared;
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

    [HttpGet("~/Warn/Guid/{id}/CreateWizard")]
    public async Task<IActionResult> CreateWarnWizard(ulong id, string? messageType = null, string? message = null)
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

        return View("CreateWarnWizard", data);
    }

    [HttpPost("~/Warn/Guid/{id}/Create")]
    public async Task<IActionResult> CreateWarn(ulong id, string user, string reason)
    {
        if (!CanAccess(id))
            return View("NotAuthorized");
        
        var guild = _discord.GetGuild(id);
        if (guild == null)
            return View("NotFound", "Guild not found");

        ulong userId = 0;
        try
        {
            userId = ulong.Parse(user);
            if (userId == null)
                throw new Exception("Parsed as null");
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to create record. Failed to parse userId: \n{ex}");
            return await CreateWarnWizard(id,
                messageType: "danger",
                message: $"Failed to create record. Failed to parse userId:  {ex.Message}");
        }

        try
        {
            var controller = Program.Core.GetRequiredService<GuildWarnItemConfigController>();
            var data = new GuildWarnItemModel()
            {
                GuildId = id,
                TargetUserId = userId,
                ActionedUserId = AspHelper.GetUserId(HttpContext) ?? 0,
                CreatedAtTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Description = reason
            };
            await controller.Add(data);
            return RedirectToAction(
                "WarnInfo", new Dictionary<string, object>()
                {
                    {
                        "id", data.WarnId
                    }
                });
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to create record. \n{ex}");
            return await CreateWarnWizard(id,
                messageType: "danger",
                message: $"Failed to create record. {ex.Message}");
        }
    }
}