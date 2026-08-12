using Discord.WebSocket;
using kate.shared.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using XeniaBot.Shared.Helpers;
using XeniaBot.Shared.Services;
using XeniaBot.WebPanel.Areas.ServerSettings.Models.BanSync;
using XeniaBot.WebPanel.Controllers;
using XeniaBot.WebPanel.Helpers;
using XeniaBot.WebPanel.Models;
using XeniaDiscord.Common.Services.BanSync;
using XeniaDiscord.Data.Models.BanSync;
using XeniaDiscord.Data.Repositories;

namespace XeniaBot.WebPanel.Areas.ServerSettings.Controllers;

[Controller]
[Area("ServerSettings")]
[Route("~/ServerSettings/BanSync")]
[AuthRequired]
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

    [Route("~/Server/{guildId}/Settings/BanSync")]
    [AuthRequired]
    [RestrictToGuild(GuildIdRouteKey = "guildId")]
    public async Task<IActionResult> BanSyncGet(ulong guildId)
    {
        var guild = _discord.GetGuild(guildId);
        if (guild == null) return PartialView("NotFoundPartial", "Guild not found");

        var model = await GetModel(guild);
        return PartialView("BanSyncComponent", model);
    }

    [HttpPost("~/Server/{guildId}/Settings/BanSync/Request")]
    [AuthRequired]
    [RestrictToGuild(GuildIdRouteKey = "guildId")]
    public async Task<IActionResult> RequestFeature(ulong guildId)
    {
        var userId = AspHelper.GetUserId(HttpContext);
        if (userId == null)
            return PartialView("NotFoundPartial", "User not found");

        var guild = ExceptionHelper.RetryOnTimedOut(() => _discord.GetGuild(guildId));
        if (guild == null)
            return PartialView("NotFoundPartial", "Guild not found");

        var configData = await _bansyncGuildRepository.GetAsync(guild.Id)
            ?? new(guild.Id);
        var logChannelId = configData.GetLogChannelId();

        var model = await GetModel(guild);

        if (logChannelId == null || logChannelId <= 1)
        {
            model.Alert = new()
            {
                MessageType = "danger",
                Message = "Unable to request Ban Sync: Log Channel not set."
            };
            return PartialView("BanSyncComponent", model);
        }

        var logChannel = guild.GetTextChannel(logChannelId.Value);
        if (logChannel == null)
        {
            model.Alert = new AlertComponentViewModel
            {
                MessageType = "danger",
                Message = $"Log Channel not found: {logChannelId}"
            };
            return PartialView("BanSyncComponent", model);
        }

        var guildKind = await _bansyncService.GetGuildKind(guild.Id);
        model.Alert = new AlertComponentViewModel
        {
            MessageType = "warning",
            Message = string.Empty
        };

        // trying to request for BanSync again
        if (configData.State != BanSyncGuildState.Unknown)
        {
            model.Alert.Message = configData.State switch
            {
                BanSyncGuildState.PendingRequest => "Ban Sync feature has already been requested",
                BanSyncGuildState.RequestDenied => "Ban Sync feature has already been requested and denied.",
                BanSyncGuildState.Blacklisted => "Your server has been blacklisted from the BanSync feature.",
                BanSyncGuildState.Active => "Your server already has BanSync enabled",
                _ => model.Alert.Message
            };
            if (string.IsNullOrEmpty(model.Alert.Message))
            {
                model.Alert.Message = $"Unable to request for BanSync feature (Unknown State: {guildKind},{configData.State})";
            }
            return PartialView("BanSyncComponent", model);
        }

        // Request ban sync
        if (configData.State == BanSyncGuildState.Unknown && guildKind == BanSyncGuildKind.Valid)
        {
            try
            {
                await _bansyncService.RequestGuildEnable(guild.Id);
                model.Alert = new()
                {
                    MessageType = "success",
                    Message = "Success! Your server is pending for approval."
                };
            }
            catch (Exception ex)
            {
                await _errorReporting.ReportException(ex, $"Failed to request ban sync access in guild {guildId}");
                model.Alert = new()
                {
                    MessageType = "danger",
                    Message = $"Unable to request Ban Sync: Failed to request.\n{ex.Message}"
                };
            }
            return PartialView("BanSyncComponent", model);
        }

        // attempting to request for BanSync, but it's not valid
        switch (guildKind)
        {
            case BanSyncGuildKind.TooYoung:
            case BanSyncGuildKind.Blacklisted:
                model.Alert = new()
                {
                    MessageType = "danger",
                    Message = $"Unable to request for BanSync, {guildKind.ToDescriptionString(guildKind.ToString())}"
                };
                break;
            case BanSyncGuildKind.LogChannelMissing:
                model.Alert = new()
                {
                    MessageType = "danger",
                    Message = $"Unable to request for BanSync, Log Channel not found: {logChannel.Name} ({logChannel.Id})"
                };
                break;
            case BanSyncGuildKind.LogChannelCannotAccess:
                model.Alert = new()
                {
                    MessageType = "danger",
                    Message = $"Unable to request for BanSync, cannot access log channel: {logChannel.Name} ({logChannel.Id})\nPlease double-check the permissions in that channel."
                };
                break;
            case BanSyncGuildKind.LogChannelCannotSendMessages:
                model.Alert = new()
                {
                    MessageType = "danger",
                    Message = $"Unable to request for BanSync, Missing Permission \"Send Messages\" in log channel: {logChannel.Name} ({logChannel.Id})"
                };
                break;
            case BanSyncGuildKind.LogChannelCannotSendEmbeds:
                model.Alert = new()
                {
                    MessageType = "danger",
                    Message = $"Unable to request for BanSync, Missing Permission \"Embed Links\" in log channel: {logChannel.Name} ({logChannel.Id})"
                };
                break;
            case BanSyncGuildKind.MissingBanMembersPermission:
                model.Alert = new()
                {
                    MessageType = "danger",
                    RenderMessageAsMarkdown = true,
                    Message = $"Unable to request for BanSync, {guildKind.ToDescriptionString(guildKind.ToString())}"
                };
                break;
            case BanSyncGuildKind.NotEnoughMembers:
                model.Alert = new()
                {
                    MessageType = "danger",
                    Message = "Unable to request for BanSync, Your server doesn't have enough members. It needs at least `35` to request the BanSync feature."
                };
                break;
        }

        return PartialView("BanSyncComponent", model);
    }

    [HttpPost("~/Server/{guildId}/Settings/BanSync")]
    [AuthRequired]
    [RestrictToGuild(GuildIdRouteKey = "guildId")]
    public async Task<IActionResult> SaveChanges(
        ulong guildId,
        string? logChannel = null)
    {
        var guild = ExceptionHelper.RetryOnTimedOut(() => _discord.GetGuild(guildId));
        if (guild == null) return PartialView("NotFoundPartial", "Guild not found");

        var model = await GetModel(guild);

        if (!ParseChannelId(logChannel, out var logResult))
        {
            model.Alert = new()
            {
                MessageType = "danger",
                Message = $"Failed to parse Ban Sync Log Channel Id.\n{logResult.ErrorContent}"
            };
            return PartialView("BanSyncComponent", model);
        }
        var guildModel = await _bansyncGuildRepository.GetAsync(guild.Id)
            ?? new(guild.Id);
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
            ?? new(guild.Id);

        return new BanSyncComponentModel()
        {
            Guild = guild,
            BanSyncGuild = dbmodel
        };
    }
}
