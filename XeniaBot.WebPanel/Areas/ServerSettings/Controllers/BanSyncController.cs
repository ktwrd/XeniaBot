using Discord.WebSocket;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
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
        var guild = ExceptionHelper.RetryOnTimedOut(() => _discord.GetGuild(guildId));
        if (guild == null) return PartialView("NotFoundPartial", "Guild not found: " + guildId);

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
            return PartialView("NotFoundPartial", "Guild not found: " + guildId);

        var configData = await _bansyncGuildRepository.GetAsync(guild.Id)
            ?? new(guild.Id);
        var logChannelId = configData.GetLogChannelId();

        var model = await GetModel(guild);
        // make sure that log channel is set
        if (logChannelId == null || logChannelId <= 1)
        {
            model.Alert = new()
            {
                MessageType = "danger",
                Message = "Unable to request Ban Sync: Log Channel not set."
            };
            return PartialView("BanSyncComponent", model);
        }

        // make sure that log channel still exists
        var logChannel = ExceptionHelper.RetryOnTimedOut(() => guild.GetTextChannel(logChannelId.Value));
        if (logChannel == null)
        {
            model.Alert = new AlertComponentViewModel
            {
                MessageType = "danger",
                Message = $"Log Channel not found: {logChannelId}"
            };
            return PartialView("BanSyncComponent", model);
        }

        var guildKind = await _bansyncService.GetGuildKind(guild);
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
        if (configData.State == BanSyncGuildState.Unknown &&
            guildKind is { GuildKind: BanSyncGuildKind.Valid, Success: true })
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
                await _errorReporting.Submit(new ErrorReportBuilder()
                    .WithException(ex)
                    .WithNotes($"Failed to request BanSync feature for guild \"{guild.Name}\" ({guildId})"));
                model.Alert = new()
                {
                    MessageType = "danger",
                    Message = "Unable to request Ban Sync: Internal Error (reported to developers)"
                };
            }
            return PartialView("BanSyncComponent", model);
        }

        // attempting to request for BanSync, but it's not valid
        model.Alert = new()
        {
            MessageType = "danger",
            RenderMessageAsMarkdown = true,
            Message = guildKind.FormatMessage(FormatMessageKind.Dashboard)
        };
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

        // check if logChannel is a valid ulong
        if (!ParseChannelId(logChannel, out var logResult))
        {
            model.Alert = new()
            {
                MessageType = "danger",
                Message = $"Failed to parse Ban Sync Log Channel Id.\n{logResult.ErrorContent}"
            };
            return PartialView("BanSyncComponent", model);
        }

        // check if the channel exists
        var textChannel = ExceptionHelper.RetryOnTimedOut(() => guild.GetTextChannel(logResult.ChannelId));
        var selfMember = ExceptionHelper.RetryOnTimedOut(() => guild.CurrentUser.GetPermissions(textChannel));
        if (textChannel == null)
        {
            model.Alert = new()
            {
                MessageType = "danger",
                Message = "Log channel not found!"
            };
            return PartialView("BanSyncComponent", model);
        }

        // check if missing permissions
        var missingPermissions = new List<string>(2);
        if (!selfMember.SendMessages) missingPermissions.Add("Send Messages");
        if (!selfMember.EmbedLinks) missingPermissions.Add("Embed Links");
        if (missingPermissions.Count > 0)
        {
            model.Alert = new()
            {
                MessageType = "danger",
                Message = "Cannot set log channel, missing one or more permissions:\n" +
                          string.Join("\n", missingPermissions.Select(e => "- " + e)),
                RenderMessageAsMarkdown = true
            };
            return PartialView("BanSyncComponent", model);
        }
        
        // otherwise, success!
        var guildModel = await _bansyncGuildRepository.GetAsync(guild.Id)
            ?? new(guild.Id);
        guildModel.LogChannelId = logResult.ChannelId.ToString();
        await _bansyncGuildRepository.InsertOrUpdate(guildModel);
        // TODO rewrite validation logic to be a method in BanSyncService to reduce duplicated code for discord commands
        model.Alert = new()
        {
            MessageType = "success",
            Message = "Successfully saved Ban Sync Log channel"
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
