using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using Discord;
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
using XeniaBot.WebPanel.Models.Component;

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
    [AuthRequired]
    [RestrictToGuild(GuildIdRouteKey = "id")]
    public async Task<IActionResult> Index(ulong id, string? messageType = null, string? message = null)
    {
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
    [AuthRequired]
    [RestrictToGuild(GuildIdRouteKey = "id")]
    public async Task<IActionResult> ModerationView(ulong id, string? messageType = null, string? message = null)
    {
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
    [AuthRequired]
    [RestrictToGuild(GuildIdRouteKey = "id")]
    public async Task<IActionResult> CountingView(ulong id, string? messageType = null, string? message = null)
    {
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
    [AuthRequired]
    [RestrictToGuild(GuildIdRouteKey = "id")]
    public async Task<IActionResult> ConfessionView(ulong id, string? messageType = null, string? message = null)
    {
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
    [AuthRequired]
    [RestrictToGuild(GuildIdRouteKey = "id")]
    public async Task<IActionResult> LevelSystemView(ulong id, string? messageType = null, string? message = null)
    {
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
    [AuthRequired]
    [RestrictToGuild(GuildIdRouteKey = "id")]
    public async Task<IActionResult> GreeterJoinView(ulong id, string? messageType = null, string? message = null)
    {
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
    [AuthRequired]
    [RestrictToGuild(GuildIdRouteKey = "id")]
    public async Task<IActionResult> GreeterLeaveView(ulong id, string? messageType = null, string? message = null)
    {
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
    [AuthRequired]
    public async Task<IActionResult> List(ListViewStyle style = ListViewStyle.List)
    {
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

        data.Items = FilterGuildsForUser(user).ToArray();
        data.ListStyle = style;
        await PopulateModel(data);
        return View("List", data);
    }

    [HttpGet("~/Server/Components/ServerList")]
    [AuthRequired]
    public async Task<IActionResult> ListComponent(int cursor = 1, ListViewStyle style = ListViewStyle.List)
    {
        var userId = AspHelper.GetUserId(HttpContext);
        if (userId == null)
        {
            return View("NotFound", "User could not be found.");
        }
        var user = _discord.GetUser((ulong)userId);
        var data = FilterGuildsForUser(user)
            .OrderByDescending(v => v.Guild.OwnerId == v.GuildUser.Id)
            .ThenByDescending(v => v.GuildUser.GuildPermissions.Has(GuildPermission.Administrator))
            .Skip((cursor - 1) * ServerListComponentViewModel.PageSize)
            .Take(ServerListComponentViewModel.PageSize)
            .ToList();
        var model = new ServerListComponentViewModel()
        {
            Items = data,
            Cursor = cursor,
            ListStyle = style
        };
        await PopulateModel(model);
        return PartialView("ServerListComponent", model);
    }

    public List<ServerListViewModelItem> FilterGuildsForUser(SocketUser user)
    {
        var dataItems = new List<ServerListViewModelItem>();
        foreach (var item in _discord.Guilds)
        {
            var guildUser = item.GetUser(user.Id);
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

        return dataItems;
    }
}