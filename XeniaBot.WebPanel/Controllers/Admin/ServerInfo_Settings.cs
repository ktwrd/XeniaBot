﻿using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using XeniaBot.Data.Repositories;
using XeniaBot.Data.Models;
using XeniaBot.Shared;
using XeniaBot.Shared.Services;
using XeniaBot.WebPanel.Helpers;
using XeniaBot.WebPanel.Models.Component;

namespace XeniaBot.WebPanel.Controllers;

public partial class AdminController
{
    /// <summary>
    /// Save Level System settings for Guild provided.
    /// </summary>
    /// <param name="id">Guild Id</param>
    /// <param name="channelId">Channel Id for Level Up notifications</param>
    /// <param name="show">Notify users when they Level Up</param>
    /// <param name="enable">Enable tracking of messages for XP</param>
    [HttpPost("~/Admin/Server/{id}/Settings/Xp")]
    [AuthRequired]
    [RequireSuperuser]
    public async Task<IActionResult> SaveSettings_Xp(ulong id, string? channelId, bool show, bool enable)
    {
        var model = new AdminLevelSystemComponentViewModel();
        await model.PopulateModel(HttpContext, id);
        ulong? targetChannelId = null;
        try
        {
            if (channelId == null)
                targetChannelId = null;
            else
                targetChannelId = ulong.Parse(channelId);
        }
        catch (Exception ex)
        {
            model.MessageType = "danger";
            model.Message = $"Invalid Channel Id ({ex.Message})";
            return PartialView("ServerInfo/LevelSystemComponent", model);
        }

        try
        {
            var controller = Program.Core.GetRequiredService<LevelSystemConfigRepository>();
            var data = await controller.Get(id) ?? new LevelSystemConfigModel()
            {
                GuildId = id,
                LevelUpChannel = targetChannelId,
                ShowLeveUpMessage = show,
                Enable = enable
            };
            data.LevelUpChannel = targetChannelId;
            data.ShowLeveUpMessage = show;
            data.Enable = enable;
            model.XpConfig = data;
            model.MessageType = "success";
            model.Message = "Saved!";
            return PartialView("ServerInfo/LevelSystemComponent", model);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to save level system config\n{ex}");
            model.MessageType = "danger";
            model.Message = ex.Message;
            return PartialView("ServerInfo/LevelSystemComponent", model);
        }
    }
    
    [HttpPost("~/Admin/Server/{id}/Settings/Confession")]
    [AuthRequired]
    [RequireSuperuser]
    public async Task<IActionResult> SaveSettings_Confession(ulong id, string? modalChannelId, string? messageChannelId)
    {
        var guild = _discord.GetGuild(id);
        if (guild == null)
            return View("NotFound", "Guild not found");

        var model = new AdminConfessionComponentViewModel();
        await model.PopulateModel(HttpContext, id);
        try
        {
            if (!ParseChannelId(modalChannelId, out var modalResult))
            {
                model.MessageType = "danger";
                model.Message = $"Failed to parse ChannelId for confession modal. ({modalResult.ErrorContent})";
                return PartialView("ServerInfo/ConfessionComponent", model);
            }
            var modalId = (ulong)modalResult.ChannelId;
        
            if (!ParseChannelId(messageChannelId, out var channelResult))
            {
                model.MessageType = "danger";
                model.Message = $"Failed to parse Message ChannelId. ({channelResult.ErrorContent})";
                return PartialView("ServerInfo/ConfessionComponent", model);
            }
            var msgId = (ulong)channelResult.ChannelId;
            
            var controller = Program.Core.GetRequiredService<ConfessionConfigRepository>();
            var data = model.ConfessionModel;
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
            model.ConfessionModel = data;
            await controller.Set(data);
            model.MessageType = "success";
            model.Message = "Saved!";
            return PartialView("ServerInfo/ConfessionComponent", model);
        }
        catch (Exception ex)
        {
            model.MessageType = "danger";
            model.Message = ex.Message;
            return PartialView("ServerInfo/ConfessionComponent", model);
        }
    }

    [HttpPost("~/Admin/Server/{id}/Settings/Confession/Purge")]
    [AuthRequired]
    [RequireSuperuser]
    public async Task<IActionResult> Confession_Purge(ulong id)
    {
        var guild = _discord.GetGuild(id);
        if (guild == null)
            return View("NotFound", "Guild not found");

        var model = new AdminConfessionComponentViewModel();
        await model.PopulateModel(HttpContext, id);
        try
        {
            var controller = Program.Core.GetRequiredService<ConfessionConfigRepository>();
            var data = await controller.GetGuild(id)
                       ?? new ConfessionGuildModel()
                       {
                           GuildId = id
                       };
            await controller.Delete(data);
            model.MessageType = "success";
            model.Message = "Purged all confession messages";
            return PartialView("ServerInfo/ConfessionComponent", model);
        }
        catch (Exception ex)
        {
            Program.Core.GetRequiredService<ErrorReportService>()
                .ReportException(ex, $"Failed to purge confession messages");
            model.MessageType = "danger";
            model.Message = ex.Message;
            return PartialView("ServerInfo/ConfessionComponent", model);
        }
    }
    
    [HttpPost("~/Admin/Server/{id}/Settings/Counting")]
    [AuthRequired]
    [RequireSuperuser]
    public async Task<IActionResult> SaveSettings_Counting(ulong id, string? inputChannelId)
    {
        var userId = AspHelper.GetUserId(HttpContext);
        if (userId == null)
            return View("NotFound", "User not found");
        var guild = _discord.GetGuild(id);
        if (guild == null)
            return View("NotFound", "Guild not found");

        ulong? channelId = null;
        try
        {
            channelId = ulong.Parse(inputChannelId ?? "0");
            if (channelId == null)
                throw new Exception("ChannelId is null");
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            return await ServerInfo(id,
                messageType: "danger",
                message: $"Failed to save Counting settings. {ex.Message}");
        }

        var controller = Program.Core.GetRequiredService<CounterConfigRepository>();
        var counterData = await controller.Get(guild) ?? new CounterGuildModel()
        {
            GuildId = guild.Id,
            ChannelId = (ulong)channelId
        };
        counterData.ChannelId = (ulong)channelId;
        await controller.Set(counterData);

        return await ServerInfo(id,
            messageType: "success",
            message: $"Counting settings saved");
    }

    [HttpPost("~/Admin/Server/{id}/Settings/RolePreserve")]
    [AuthRequired]
    [RequireSuperuser]
    public async Task<IActionResult> SaveSettings_RolePreserve(ulong id, bool enable)
    {
        var guild = _discord.GetGuild(id);
        if (guild == null)
            return View("NotFound", "Guild not found");

        try
        {
            var controller = Program.Core.GetRequiredService<RolePreserveGuildRepository>();
            var data = await controller.Get(id) ?? new RolePreserveGuildModel()
            {
                GuildId = guild.Id
            };
            data.Enable = enable;
            await controller.Set(data);
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            return await ServerInfo(id,
                messageType: "danger",
                message: $"Failed to save Role Preserve settings. {ex.Message}");
        }

        return await ServerInfo(id,
            messageType: "success",
            message: $"Role Preserve settings saved.");
    }
}