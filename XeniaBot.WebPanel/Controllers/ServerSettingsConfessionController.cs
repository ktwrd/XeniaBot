﻿using Microsoft.AspNetCore.Mvc;
using XeniaBot.Data.Repositories;
using XeniaBot.Data.Models;
using XeniaBot.Shared.Services;

namespace XeniaBot.WebPanel.Controllers;

public partial class ServerController
{
    [HttpPost("~/Server/{id}/Settings/Confession")]
    public async Task<IActionResult> SaveSettings_Confession(ulong id, string? modalChannelId, string? messageChannelId)
    {
        if (!CanAccess(id))
            return View("NotAuthorized");
        
        var guild = _discord.GetGuild(id);
        if (guild == null)
            return View("NotFound", "Guild not found");
        
        var modalChannelIdRes = ParseChannelId(modalChannelId);
        if (modalChannelIdRes.ErrorContent != null)
        {
            return await ConfessionView(id,
                messageType: "danger",
                message: $"Failed to parse ChannelId for confession modal. {modalChannelIdRes.ErrorContent}");
        }
        var modalId = (ulong)modalChannelIdRes.ChannelId;
        
        var msgChannelIdRes = ParseChannelId(messageChannelId);
        if (msgChannelIdRes.ErrorContent != null)
        {
            return await ConfessionView(id,
                messageType: "danger",
                message: $"Failed to parse ChannelId for messages. {msgChannelIdRes.ErrorContent}");
        }
        var msgId = (ulong)msgChannelIdRes.ChannelId;

        try
        {
            var controller = Program.Core.GetRequiredService<ConfessionConfigRepository>();
            var data = await controller.GetGuild(id) ??
                       new ConfessionGuildModel()
                       {
                           GuildId = id
                       };
            if (data.ModalChannelId != modalId)
            {
                await controller.InitializeModal(id, msgId, modalId);
            }

            data = await controller.GetGuild(id) ??
                   new ConfessionGuildModel()
                   {
                       GuildId = id
                   };
            data.ModalChannelId = modalId;
            data.ChannelId = msgId;
            await controller.Set(data);
        }
        catch (Exception ex)
        {
            Program.Core.GetRequiredService<ErrorReportService>()
                .ReportException(ex, $"Failed to save confession settings");
            return await ConfessionView(id,
                messageType: "danger",
                message: $"Failed to save settings. {ex.Message}");
        }
        return await ConfessionView(id,
            messageType: "success",
            message: $"Saved settings");
    }

    [HttpGet("~/Server/{id}/Settings/Confession/Purge")]
    public async Task<IActionResult> SaveSettings_Confession_Purge(ulong id)
    {
        if (!CanAccess(id))
            return View("NotAuthorized");
        
        var guild = _discord.GetGuild(id);
        if (guild == null)
            return View("NotFound", "Guild not found");

        try
        {
            var controller = Program.Core.GetRequiredService<ConfessionConfigRepository>();
            var data = await controller.GetGuild(id)
                ?? new ConfessionGuildModel()
                {
                   GuildId = id
                };
            await controller.Delete(data);
            return await Index(id,
                messageType: "success",
                message: $"Successfully purged confession messages");
        }
        catch (Exception ex)
        {
            Program.Core.GetRequiredService<ErrorReportService>()
                .ReportException(ex, $"Failed to purge confession messages");
            return await Index(id,
                messageType: "danger",
                message: $"Failed to purge confession messages. {ex.Message}");
        }
    }
}