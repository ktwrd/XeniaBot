using System;
using Discord.WebSocket;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XeniaBot.MongoData.Repositories;
using XeniaBot.Shared;
using XeniaBot.Shared.Services;
using XeniaBot.WebPanel.Helpers;
using XeniaBot.WebPanel.Models;
using XeniaDiscord.Common.Services.BanSync;
using XeniaDiscord.Data;

using RolePreserveGuildRepository = XeniaDiscord.Data.Repositories.RolePreserveGuildRepository;

namespace XeniaBot.WebPanel.Controllers;

[Controller]
public partial class AdminController : BaseXeniaController
{
    private readonly ILogger<AdminController> _logger;
    private readonly IServiceProvider _services;
    private readonly DiscordShardedClient _client;
    private readonly XeniaDbContext _db;
    private readonly IDbContextFactory<XeniaDbContext> _dbContextFactory;
    private readonly RolePreserveGuildRepository _rolePreserveGuildRepo;
    private readonly ConfigData _config;

    private readonly BanSyncService _banSyncService;
    private readonly ErrorReportService _errorReportService;

    private readonly LevelSystemConfigRepository _levelSystemConfigRepository;
    private readonly ConfessionConfigRepository _confessionConfigRepository;
    private readonly CounterConfigRepository _counterConfigRepository;
    
    public AdminController(
        IServiceProvider services,
        ILogger<AdminController> logger)
        : base(services)
    {
        _logger = logger;
        _services = services;

        _errorReportService = services.GetRequiredService<ErrorReportService>();
        _banSyncService = services.GetRequiredService<BanSyncService>();
        _levelSystemConfigRepository = services.GetRequiredService<LevelSystemConfigRepository>();
        _confessionConfigRepository = services.GetRequiredService<ConfessionConfigRepository>();
        _counterConfigRepository = services.GetRequiredService<CounterConfigRepository>();
        
        _client = _services.GetRequiredService<DiscordShardedClient>();
        _config = _services.GetRequiredService<ConfigData>();
        _db = services.GetRequiredService<XeniaDbContext>();
        _dbContextFactory = services.GetRequiredService<IDbContextFactory<XeniaDbContext>>();
        _rolePreserveGuildRepo = services.GetRequiredService<RolePreserveGuildRepository>();
    }
    
    public override bool CanAccess(out IActionResult? result)
    {
        if (User?.Identity?.IsAuthenticated != true)
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