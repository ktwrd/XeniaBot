using Discord.WebSocket;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using XeniaBot.WebPanel.Areas.Admin.Models.Guild;
using XeniaDiscord.Data;

namespace XeniaBot.WebPanel.Areas.Admin.Controllers;

[Controller]
[Area("Admin")]
[Route("~/Admin/[controller]")]
[AuthRequired]
[RequireSuperuser]
public class GuildController : Controller
{
    private readonly ILogger<GuildController> _logger;
    private readonly DiscordSocketClient _client;
    private readonly XeniaDbContext _db;
    public GuildController(
        IServiceProvider services,
        ILogger<GuildController> logger)
    {
        _logger = logger;
        _client = services.GetRequiredService<DiscordSocketClient>();
        _db = services.GetRequiredService<XeniaDbContext>();
    }

    [AuthRequired]
    [RequireSuperuser]
    [HttpGet("List")]
    public async Task<IActionResult> List()
    {
        var items = new List<ListModelItem>();
        foreach (var guild in _client.Guilds.OrderByDescending(e => e.CurrentUser.JoinedAt))
        {
            items.Add(new ListModelItem
            {
                Id = guild.Id,
                Name = guild.Name,
                IconUrl = guild.IconUrl,
                IsMember = true,
                RecordLastUpdatedAt = null,
                MemberCount = guild.MemberCount,
                ChannelCount = guild.Channels.Count,
                RoleCount = guild.Roles.Count,
                JoinedAt = guild.CurrentUser.JoinedAt?.UtcDateTime,
            });
        }
        var guildIds = _client.Guilds
            .Select(e => e.Id.ToString()).ToArray();
        var cacheGuilds = await _db.GuildCache
            .AsNoTracking()
            .Where(e => !guildIds.Contains(e.Id))
            .ToListAsync();
        foreach (var guild in cacheGuilds)
        {
            items.Add(new ListModelItem
            {
                Id = guild.GetGuildId(),
                Name = guild.Name,
                IconUrl = guild.IconUrl,
                IsMember = false,
                RecordLastUpdatedAt = guild.RecordUpdatedAt,
                JoinedAt = guild.JoinedAt,
            });
        }

        var model = new ListModel
        {
            Items = items
        };
        return View("List", model);
    }
}
