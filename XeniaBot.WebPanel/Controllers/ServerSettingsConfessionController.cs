using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using XeniaBot.Data.Repositories;
using XeniaBot.Data.Models;
using XeniaBot.Shared.Services;

namespace XeniaBot.WebPanel.Controllers;

public partial class ServerController
{
    [HttpPost("~/Server/{id}/Settings/Confession")]
    [AuthRequired]
    [RestrictToGuild(GuildIdRouteKey = "id")]
    public async Task<IActionResult> SaveSettings_Confession(ulong id, string? modalChannelId, string? messageChannelId)
    {
        var guild = _discord.GetGuild(id);
        if (guild == null)
            return View("NotFound", "Guild not found");

        if (!ParseChannelId(modalChannelId, out var modalResult))
        {
            return await ConfessionView(id,
                messageType: "danger",
                message: $"Failed to parse ChannelId for confession modal. {modalResult.ErrorContent}");
        }
        var modalId = (ulong)modalResult.ChannelId;

        if (!ParseChannelId(messageChannelId, out var channelResult))
        {
            return await ConfessionView(id,
                messageType: "danger",
                message: $"Failed to parse ChannelId for messages. {channelResult.ErrorContent}");
        }
        var msgId = (ulong)channelResult.ChannelId;

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
    [AuthRequired]
    [RestrictToGuild(GuildIdRouteKey = "id")]
    public async Task<IActionResult> SaveSettings_Confession_Purge(ulong id)
    {
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