using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Data.Repositories;
using XeniaBot.Data.Models;
using XeniaBot.Data;
using XeniaBot.Data.Services;
using XeniaBot.Shared.Services;
using XeniaBot.WebPanel.Helpers;

namespace XeniaBot.WebPanel.Controllers;

public partial class ServerController
{
    [HttpPost("~/Server/{id}/Settings/BanSync/Request")]
    public async Task<IActionResult> SaveSettings_BanSync_Request(ulong id)
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
        
        var controller = Program.Core.Services.GetRequiredService<BanSyncConfigRepository>();
        var configData = await controller.Get(guild.Id) ?? new ConfigBanSyncModel()
        {
            GuildId = guild.Id
        };

        if (configData.LogChannel == 0)
        {
            return await ModerationView(id,
                messageType: "danger",
                message: $"Unable to request Ban Sync: Log Channel not set.");
        }

        try
        {
            if (guild.GetTextChannel(configData.LogChannel) == null)
                throw new Exception("Not found");
        }
        catch (Exception ex)
        {
            Program.Core.Services.GetRequiredService<ErrorReportService>()
                .ReportException(ex, $"Failed to get log channel");
            return await ModerationView(id,
                messageType: "danger",
                message: $"Unable to request Ban Sync: Failed to get log channel: {ex.Message}");
        }
        
        switch (configData.State)
        {
            case BanSyncGuildState.PendingRequest:
                return await ModerationView(id,
                    messageType: "danger",
                    message: "Ban Sync access has already been requested");
                break;
            case BanSyncGuildState.RequestDenied:
                return await ModerationView(id,
                    messageType: "danger",
                    message: "Ban Sync access has already been requested and denied.");
            case BanSyncGuildState.Blacklisted:
                return await ModerationView(id,
                    messageType: "danger",
                    message: $"Your server has been blacklisted");
                break;
            case BanSyncGuildState.Active:
                return await ModerationView(id,
                    messageType: "danger",
                    message: $"Your server already has Ban Sync enabled");
                break;
            case BanSyncGuildState.Unknown:
                // Request ban sync
                try
                {
                    var dcon = Program.Core.Services.GetRequiredService<BanSyncService>();
                    if (dcon == null)
                        throw new Exception($"Failed to get BanSyncService");

                    var res = await dcon.RequestGuildEnable(guild.Id);
                    return await ModerationView(id,
                        messageType: "success",
                        message: $"Ban Sync: Your server is pending approval");
                }
                catch (Exception ex)
                {
                    Program.Core.Services.GetRequiredService<ErrorReportService>()
                        .ReportException(ex, $"Failed to request ban sync access in guild {id}");
                    return await ModerationView(id,
                        messageType: "danger",
                        message: $"Unable to request Ban Sync: Failed to request. {ex.Message}");
                }
                break;
        }
        return await ModerationView(id,
            messageType: "warning",
            message: $"Ban Sync Fail: Unhandled state {configData.State}");
    }
    [HttpPost("~/Server/{id}/Settings/BanSync")]
    public async Task<IActionResult> SaveSettings_BanSync(
        ulong id,
        string? logChannel = null)
    {
        if (!CanAccess(id))
            return View("NotAuthorized");
        
        var guild = _discord.GetGuild(id);
        if (guild == null)
            return View("NotFound", "Guild not found");

        var channelIdResult = ParseChannelId(logChannel);
        if (channelIdResult.ErrorContent != null)
        {
            return await ModerationView(id,
                messageType: "danger",
                message: $"Failed to parse Ban Sync Log Channel Id. {channelIdResult.ErrorContent}");
        }
        var channelId = (ulong)channelIdResult.ChannelId;
        
        var controller = Program.Core.Services.GetRequiredService<BanSyncConfigRepository>();
        var configData = await controller.Get(guild.Id) ?? new ConfigBanSyncModel()
        {
            GuildId = guild.Id,
            LogChannel = channelId
        };
        configData.LogChannel = channelId;
        await controller.Set(configData);

        return await ModerationView(id,
            messageType: "success",
            message: $"Successfully saved Ban Sync Log channel");
    }
}