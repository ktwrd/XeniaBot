using System;
using Discord.WebSocket;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XeniaBot.Shared;
using XeniaBot.WebPanel.Helpers;
using XeniaBot.WebPanel.Models;

namespace XeniaBot.WebPanel.Controllers;

[Controller]
public partial class AdminController : BaseXeniaController
{
    private readonly ILogger<AdminController> _logger;
    private readonly IServiceProvider _services;
    private readonly DiscordSocketClient _client;
    private readonly ConfigData _config;
    public AdminController(ILogger<AdminController> logger)
        : base()
    {
        _logger = logger;
        _services = Program.Core.Services;

        _client = _services.GetRequiredService<DiscordSocketClient>();
        _config = _services.GetRequiredService<ConfigData>();
    }
    
    public override bool CanAccess(out IActionResult? result)
    {
        bool isAuth = User?.Identity?.IsAuthenticated ?? false;
        if (!isAuth)
        {
            result = View("NotAuthorized", new NotAuthorizedViewModel()
            {
                ShowLoginButton = true
            });
            return false;
        }
        var userId = AspHelper.GetUserId(HttpContext);
        if (userId == null)
        {
            result = View("NotAuthorized");
            return false;
        }

        if (!_config.UserWhitelist.Contains((ulong)userId))
        {
            result = View("NotAuthorized");
            return false;
        }

        result = null;
        return true;
    }

    public override bool CanAccess(ulong guildId, out IActionResult? result) => CanAccess(out result);
    public override bool CanAccess(ulong guildId, ulong userId, out IActionResult? result) => CanAccess(out result);
}