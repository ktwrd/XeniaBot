using Discord.WebSocket;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using XeniaBot.WebPanel.Models.BanSync;
using XeniaBot.WebPanel.Models.BanSyncSearch;
using XeniaDiscord.Data;
using XeniaDiscord.Data.Models.BanSync;
using XeniaDiscord.Data.Repositories;

namespace XeniaBot.WebPanel.Controllers;

[Controller]
[Route("~/BanSync/Search")]
public class BanSyncSearchController : BaseXeniaController
{
    private readonly XeniaDbContext _db;
    private readonly BanSyncGuildRepository _bansyncGuildRepo;
    private readonly DiscordSocketClient _discord;
    public BanSyncSearchController(IServiceProvider services)
    {
        _db = services.GetRequiredService<XeniaDbContext>();
        _bansyncGuildRepo = services.GetRequiredService<BanSyncGuildRepository>();
        _discord = services.GetRequiredService<DiscordSocketClient>();
    }

    [HttpGet("MutualRecords/{guildId}")]
    [HttpPost("MutualRecords/{guildId}")]
    public async Task<IActionResult> MutualRecords(
        ulong guildId,
        BanSyncMutualRecordsQuery query)
    {
        var guild = _discord.GetGuild(guildId);
        if (guild == null)
        {
            return View("NotFound", $"Guild not found: {guildId}");
        }

        var guildModel = await _bansyncGuildRepo.GetAsync(guild.Id);
        if (guildModel?.Enable != true || guildModel.State != BanSyncGuildState.Active)
        {
            return View("BanSyncNotEnabled", new BanSyncNotEnabledModel
            {
                Guild = guild
            });
        }

        throw new NotImplementedException();
    }

    [HttpPost("Perform")]
    public async Task<IActionResult> PerformSearch(BanSyncSearchQuery query)
    {
        throw new NotImplementedException();
    }
}
