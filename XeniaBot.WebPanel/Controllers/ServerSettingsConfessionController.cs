using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using XeniaBot.Data.Repositories;
using XeniaBot.Data.Models;
using XeniaBot.Shared.Services;

namespace XeniaBot.WebPanel.Controllers;

public partial class ServerController
{
    #region Save
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

    [HttpPost("~/Server/{id}/Settings/Confession/Component")]
    [AuthRequired]
    [RestrictToGuild(GuildIdRouteKey = "id")]
    public async Task<IActionResult> SaveSettings_ConfessionComponent(ulong id,
        string? modalChannelId,
        string? messageChannelId)
    {
        var guild = _discord.GetGuild(id);
        if (guild == null)
            return View("NotFound", "Guild not found");

        var model = await GetDetails(guild.Id);
        
        if (!ParseChannelId(modalChannelId, out var modalResult))
        {
            model.MessageType = "danger";
            model.Message = $"Failed to parse ChannelId for confession modal. {modalResult.ErrorContent}";
            return PartialView("Details/FunView/ConfessionComponentView", model);
        }
        var modalId = (ulong)modalResult.ChannelId;

        if (!ParseChannelId(messageChannelId, out var channelResult))
        {
            model.MessageType = "danger";
            model.Message = $"Failed to parse ChannelId for messages. {channelResult.ErrorContent}";
            return PartialView("Details/FunView/ConfessionComponentView", model);
        }
        var msgId = (ulong)channelResult.ChannelId;

        var controller = Program.Core.GetRequiredService<ConfessionConfigRepository>();
        var data = await controller.GetGuild(id) ??
                   new ConfessionGuildModel()
                   {
                       GuildId = id
                   };
        bool initialize = data.ModalChannelId != modalId;
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
        model.ConfessionConfig = data;
        model.MessageType = "success";
        model.Message = "Saved Settings" + (initialize ? " (Created Modal)" : "");
        return PartialView("Details/FunView/ConfessionComponentView");
    }
    #endregion
    
    #region Purge
    [HttpGet("~/Server/{id}/Settings/Confession/Purge")]
    [AuthRequired]
    [RestrictToGuild(GuildIdRouteKey = "id")]
    public async Task<IActionResult> SaveSettings_Confession_Purge(ulong id)
    {
        var guild = _discord.GetGuild(id);
        if (guild == null)
            return View("NotFound", "Guild not found");

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

    [HttpGet("~/Server/{id}/Settings/Confession/Component/Purge")]
    [AuthRequired]
    [RestrictToGuild(GuildIdRouteKey = "id")]
    public async Task<IActionResult> SaveSettings_Confession_PurgeComponent(ulong id)
    {
        var guild = _discord.GetGuild(id);
        if (guild == null)
            return PartialView("NotFound", "Guild not found");

        var model = await GetDetails(guild.Id);
        var controller = Program.Core.GetRequiredService<ConfessionConfigRepository>();
        var data = await controller.GetGuild(id)
                   ?? new ConfessionGuildModel()
                   {
                       GuildId = id
                   };
        await controller.Delete(data);
        model.MessageType = "success";
        model.Message = "Purged all messages";
        return PartialView("Details/FunView/ConfessionComponentView", model);
    }
    #endregion
}