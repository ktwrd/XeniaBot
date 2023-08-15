using Discord.WebSocket;
using Microsoft.AspNetCore.Mvc;
using XeniaBot.Shared;
using XeniaBot.WebPanel.Helpers;
using XeniaBot.WebPanel.Models;

namespace XeniaBot.WebPanel.Controllers;

public partial class AdminController : Controller
{
    private readonly IServiceProvider services;
    private readonly DiscordSocketClient _client;
    private readonly ConfigData _config;
    public AdminController()
        : base()
    {
        services = Program.Services;

        _client = services.GetRequiredService<DiscordSocketClient>();
        _config = services.GetRequiredService<ConfigData>();
    }
    
    public bool CanAccess()
    {
        bool isAuth = User?.Identity?.IsAuthenticated ?? false;
        if (!isAuth)
            return false;
        var userId = AspHelper.GetUserId(HttpContext);
        if (userId == null)
        {
            return false;
        }

        return _config.UserWhitelist.Contains((ulong)userId);
    }
}