using Discord.WebSocket;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using XeniaBot.Shared.Services;
using XeniaBot.WebPanel.Areas.ServerSettings.Models.BanSync;
using XeniaBot.WebPanel.Controllers;
using XeniaBot.WebPanel.Helpers;
using XeniaDiscord.Common.Services;
using XeniaDiscord.Data.Models.BanSync;
using XeniaDiscord.Data.Repositories;

namespace XeniaBot.WebPanel.Areas.ServerSettings.Controllers;

[Controller]
[Area("ServerSettings")]
public class BanSyncController : BaseXeniaController
{
    private readonly ILogger<BanSyncController> _logger;
    private readonly BanSyncGuildRepository _bansyncGuildRepository;
    private readonly ErrorReportService _errorReporting;
    private readonly BanSyncService _bansyncService;
    public BanSyncController(IServiceProvider services,
         ILogger<BanSyncController> logger)
    {
        _logger = logger;
        _bansyncGuildRepository = services.GetRequiredService<BanSyncGuildRepository>();
        _errorReporting = services.GetRequiredService<ErrorReportService>();
        _bansyncService = services.GetRequiredService<BanSyncService>();
    }

    [HttpGet("~/Server/{id}/Settings/BanSync")]
    [AuthRequired]
    [RestrictToGuild(GuildIdRouteKey = "guildId")]
    public async Task<IActionResult> BanSyncGet(ulong guildId)
    {
        var guild = _discord.GetGuild(guildId);
        if (guild == null) return PartialView("NotFoundPartial", "Guild not found");

        var model = await GetModel(guild);
        return PartialView("BanSyncComponent", model);
    }

    [HttpPost("~/Server/{id}/Settings/BanSync/Request")]
    [AuthRequired]
    [RestrictToGuild(GuildIdRouteKey = "id")]
    public async Task<IActionResult> Request(ulong id)
    {
        var userId = AspHelper.GetUserId(HttpContext);
        if (userId == null)
            return PartialView("NotFoundPartial", "User not found");

        var guild = _discord.GetGuild(id);
        if (guild == null)
            return PartialView("NotFoundPartial", "Guild not found");

        var configData = await _bansyncGuildRepository.GetAsync(guild.Id)
            ?? new()
            {
                GuildId = guild.Id.ToString()
            };
        var logChannelId = configData.GetLogChannelId();

        var model = await GetModel(guild);

        if (logChannelId == null || logChannelId == 0)
        {
            model.Alert = new()
            {
                MessageType = "danger",
                Message = "Unable to request Ban Sync: Log Channel not set."
            };
            return PartialView("BanSyncComponent", model);
        }
        if (guild.GetTextChannel(logChannelId.Value) == null)
        {
            model.Alert = new()
            {
                MessageType = "danger",
                Message = $"Log Channel not found: {logChannelId}"
            };
            return PartialView("BanSyncComponent", model);
        }

        switch (configData.State)
        {
            case BanSyncGuildState.PendingRequest:
                model.Alert = new()
                {
                    MessageType = "danger",
                    Message = "Ban Sync access has already been requested"
                };
                break;
            case BanSyncGuildState.RequestDenied:
                model.Alert = new()
                {
                    MessageType = "danger",
                    Message = "Ban Sync access has already been requested and denied."
                };
                break;
            case BanSyncGuildState.Blacklisted:
                model.Alert = new()
                {
                    MessageType = "danger",
                    Message = $"Your server has been blacklisted"
                };
                break;
            case BanSyncGuildState.Active:
                model.Alert = new()
                {
                    MessageType = "danger",
                    Message = $"Your server already has Ban Sync enabled"
                };
                break;
            case BanSyncGuildState.Unknown:
                // Request ban sync
                try
                {
                    await _bansyncService.RequestGuildEnable(guild.Id);
                    model.Alert = new()
                    {
                        MessageType = "success",
                        Message = $"Ban Sync: Your server is pending approval"
                    };
                }
                catch (Exception ex)
                {
                    await _errorReporting.ReportException(ex, $"Failed to request ban sync access in guild {id}");
                    model.Alert = new()
                    {
                        MessageType = "danger",
                        Message = $"Unable to request Ban Sync: Failed to request.\n{ex.Message}"
                    };
                }
                break;
            default:
                model.Alert = new()
                {
                    MessageType = "warning",
                    Message = $"Ban Sync Fail: Unhandled state {configData.State}"
                };
                break;
        }
        return PartialView("BanSyncComponent", model);
    }

    [HttpPost("~/Server/{id}/Settings/BanSync")]
    [AuthRequired]
    [RestrictToGuild(GuildIdRouteKey = "id")]
    public async Task<IActionResult> SaveChanges(
        ulong id,
        string? logChannel = null)
    {
        var guild = _discord.GetGuild(id);
        if (guild == null) return PartialView("NotFoundPartial", "Guild not found");

        var model = await GetModel(guild);

        if (!ParseChannelId(logChannel, out var logResult))
        {
            model.Alert = new()
            {
                MessageType = "danger",
                Message = $"Failed to parse Ban Sync Log Channel Id. {logResult.ErrorContent}"
            };
            return PartialView("BanSyncComponent", model);
        }
        var guildModel = await _bansyncGuildRepository.GetAsync(guild.Id) ?? new()
        {
            GuildId = guild.Id.ToString()
        };
        guildModel.LogChannelId = logResult.ChannelId.ToString();
        await _bansyncGuildRepository.InsertOrUpdate(guildModel);

        model.Alert = new()
        {
            MessageType = "success",
            Message = $"Successfully saved Ban Sync Log channel"
        };
        return PartialView("BanSyncComponent", model);
    }

    private async Task<BanSyncComponentModel> GetModel(SocketGuild guild)
    {
        var dbmodel = await _bansyncGuildRepository.GetAsync(guild.Id)
            ?? new()
            {
                GuildId = guild.Id.ToString()
            };

        return new BanSyncComponentModel()
        {
            Guild = guild,
            BanSyncGuild = dbmodel
        };
    }
}
