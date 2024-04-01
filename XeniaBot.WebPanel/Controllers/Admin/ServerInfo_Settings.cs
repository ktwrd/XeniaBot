﻿using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using XeniaBot.Data.Repositories;
using XeniaBot.Data.Models;
using XeniaBot.Shared;
using XeniaBot.WebPanel.Helpers;

namespace XeniaBot.WebPanel.Controllers;

public partial class AdminController
{
    [HttpPost("~/Admin/Server/{id}/Settings/Xp")]
    [AuthRequired(RequireWhitelist = true)]
    public async Task<IActionResult> SaveSettings_Xp(ulong id, string? channelId, bool show, bool enable)
    {
        var guild = _discord.GetGuild(id);
        if (guild == null)
            return View("NotFound", "Level System: Guild not found");

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
            return await ServerInfo(id,
                messageType: "danger",
                message: $"Failed to save Level System settings. {ex.Message}");
        }

        try
        {
            var controller = Program.Core.GetRequiredService<LevelSystemConfigRepository>();
            var data = await controller.Get(guild.Id) ?? new LevelSystemConfigModel()
            {
                GuildId = guild.Id,
                LevelUpChannel = targetChannelId,
                ShowLeveUpMessage = show,
                Enable = enable
            };
            data.LevelUpChannel = targetChannelId;
            data.ShowLeveUpMessage = show;
            data.Enable = enable;
            await controller.Set(data);
            return await ServerInfo(id,
                messageType: "success",
                message: $"Level System Settings Saved");
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to save level system config\n{ex}");
            return await ServerInfo(id,
                messageType: "danger",
                message: $"Failed to save Level System settings. {ex.Message}");
        }
    }
    
    [HttpPost("~/Admin/Server/{id}/Settings/Confession")]
    [AuthRequired(RequireWhitelist = true)]
    public async Task<IActionResult> SaveSettings_Confession(ulong id, string? modalChannelId, string? messageChannelId)
    {
        var guild = _discord.GetGuild(id);
        if (guild == null)
            return View("NotFound", "Guild not found");
        
        var modalChannelIdRes = ParseChannelId(modalChannelId);
        if (modalChannelIdRes.ErrorContent != null)
        {
            return await ServerInfo(id,
                messageType: "danger",
                message: $"Failed to parse ChannelId for confession modal. {modalChannelIdRes.ErrorContent}");
        }
        var modalId = (ulong)modalChannelIdRes.ChannelId;
        
        var msgChannelIdRes = ParseChannelId(messageChannelId);
        if (msgChannelIdRes.ErrorContent != null)
        {
            return await ServerInfo(id,
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
            return await ServerInfo(id,
                messageType: "danger",
                message: $"Failed to save confession settings. {ex.Message}");
        }
        return await ServerInfo(id,
            messageType: "success",
            message: $"Successfully saved Ban Sync Log channel");
    }
    
    [HttpPost("~/Admin/Server/{id}/Settings/Counting")]
    [AuthRequired(RequireWhitelist = true)]
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
    [AuthRequired(RequireWhitelist = true)]
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