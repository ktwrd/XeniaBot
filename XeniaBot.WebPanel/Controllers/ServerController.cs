using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Net;
using System.Web;
using Discord.WebSocket;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using XeniaBot.Data.Controllers.BotAdditions;
using XeniaBot.Data.Models;
using XeniaBot.WebPanel.Extensions;
using XeniaBot.WebPanel.Helpers;
using XeniaBot.WebPanel.Models;

namespace XeniaBot.WebPanel.Controllers;

public partial class ServerController : Controller
{
    private readonly ILogger<ServerController> _logger;
    private readonly DiscordSocketClient _discord;

    public ServerController(ILogger<ServerController> logger)
    {
        _logger = logger;
        _discord = Program.Services.GetRequiredService<DiscordSocketClient>();
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
        data.MessageType = messageType;
        data.Message = message;

        if (data.MessageType != null)
        {
            var valid = new string[]
            {
                "primary",
                "secondary",
                "success",
                "danger",
                "warning",
                "info"
            };
            if (!valid.Contains(data.MessageType))
                data.MessageType = "primary";
        }

        if (data.MessageType == null)
            data.MessageType = "primary";
        
        return View("Details", data);
    }

    
    [HttpGet("~/Server/")]
    [HttpGet("~/Server/List")]
    public async Task<IActionResult> List()
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
        return View("List", data);
    }
}