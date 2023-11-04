using Discord.WebSocket;
using Microsoft.AspNetCore.Mvc;
using XeniaBot.Shared;
using XeniaBot.WebPanel.Helpers;
using XeniaBot.WebPanel.Models;

namespace XeniaBot.WebPanel.Controllers;

[Controller]
public partial class AdminController : BaseXeniaController
{
    private readonly IServiceProvider _services;
    private readonly DiscordSocketClient _client;
    private readonly ConfigData _config;
    public AdminController()
        : base()
    {
        _services = Program.Services;

        _client = _services.GetRequiredService<DiscordSocketClient>();
        _config = _services.GetRequiredService<ConfigData>();
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

    public override bool CanAccess(ulong guildId) => CanAccess();
    public override bool CanAccess(ulong guildId, ulong userId) => CanAccess();
}