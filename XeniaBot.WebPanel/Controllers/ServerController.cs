using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using Discord.WebSocket;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using XeniaBot.Data;
using XeniaBot.Data.Repositories;
using XeniaBot.Data.Models;
using XeniaBot.WebPanel.Extensions;
using XeniaBot.WebPanel.Helpers;
using XeniaBot.WebPanel.Models;

namespace XeniaBot.WebPanel.Controllers;

[Controller]
public partial class ServerController : BaseXeniaController
{
    private readonly ILogger<ServerController> _logger;

    public ServerController(ILogger<ServerController> logger)
        : base()
    {
        _logger = logger;
    }

    [HttpGet("~/Server/{id}")]
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
        data.User = guildUser;
        
        await PopulateModel(data);
        if (messageType != null)
            data.MessageType = messageType;
        if (message != null)
            data.Message = message;
        
        return View("Details", data);
    }

    [HttpGet("~/Server/{id}/Moderation")]
    public async Task<IActionResult> ModerationView(ulong id, string? messageType = null, string? message = null)
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
        
        return View("Details/ModerationView", data);
    }
    [HttpGet("~/Server/{id}/Fun/Counting")]
    public async Task<IActionResult> CountingView(ulong id, string? messageType = null, string? message = null)
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
        
        return View("Details/FunView/CountingView", data);
    }
    [HttpGet("~/Server/{id}/Fun/Confession")]
    public async Task<IActionResult> ConfessionView(ulong id, string? messageType = null, string? message = null)
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
        
        return View("Details/FunView/ConfessionView", data);
    }
    [HttpGet("~/Server/{id}/Fun/LevelSystem")]
    public async Task<IActionResult> LevelSystemView(ulong id, string? messageType = null, string? message = null)
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
        
        return View("Details/FunView/LevelSystemView", data);
    }
    
    [HttpGet("~/Server/{id}/Greeter/Join")]
    public async Task<IActionResult> GreeterJoinView(ulong id, string? messageType = null, string? message = null)
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
        
        return View("Details/Settings/GreeterJoinView", data);
    }
    [HttpGet("~/Server/{id}/Greeter/Leave")]
    public async Task<IActionResult> GreeterLeaveView(ulong id, string? messageType = null, string? message = null)
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
        
        return View("Details/Settings/GreeterLeaveView", data);
    }

    
    [HttpGet("~/Server/")]
    [HttpGet("~/Server/List")]
    public async Task<IActionResult> List(ListViewStyle style = ListViewStyle.List)
    {
        bool isAuth = User?.Identity?.IsAuthenticated ?? false;
        if (!isAuth)
            return View("NotAuthorized");

        var userId = AspHelper.GetUserId(HttpContext);
        if (userId == null)
        {
            return View("NotFound", "User could not be found.");
        }
        var user = _discord.GetUser((ulong)userId);
        var data = new ServerListViewModel()
        {
            UserId = (ulong)userId,
            UserAvatar = user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl()
        };
        var dataItems = new List<ServerListViewModelItem>();
        foreach (var item in _discord.Guilds)
        {
            var guildUser = item.GetUser((ulong)userId);
            if (guildUser == null)
                continue;
            if (!guildUser.GuildPermissions.ManageGuild)
                continue;
            dataItems.Add(new ServerListViewModelItem()
            {
                Guild = item,
                GuildUser = guildUser
            });
        }

        data.Items = dataItems.ToArray();
        data.ListStyle = style;
        await PopulateModel(data);
        return View("List", data);
    }
}