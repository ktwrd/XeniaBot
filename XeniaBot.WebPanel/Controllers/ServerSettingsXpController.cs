using Microsoft.AspNetCore.Mvc;
using XeniaBot.Data.Controllers.BotAdditions;
using XeniaBot.Data.Models;
using XeniaBot.Shared;

namespace XeniaBot.WebPanel.Controllers;

public partial class ServerController
{

    [HttpPost("~/Server/{id}/Settings/Xp/Save")]
    public async Task<IActionResult> SaveSettings_Xp(ulong id, string? channelId, bool show, bool enable)
    {
        if (!CanAccess(id))
            return View("NotAuthorized");
        
        var guild = _discord.GetGuild(id);
        if (guild == null)
            return View("NotFound", "Guild not found");

        ulong? targetChannelId;
        try
        {
            if (channelId == null)
                targetChannelId = null;
            else
                targetChannelId = ulong.Parse(channelId);
        }
        catch (Exception e)
        {
            return await Index(id,
                messageType: "danger",
                message: $"Failed to save Level System settings. {e.Message}");
        }

        try
        {
            var controller = Program.Services.GetRequiredService<LevelSystemGuildConfigController>();
            var data = await controller.Get(guild.Id) ?? new LevelSystemGuildConfigModel()
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
            return await Index(id,
                messageType: "success",
                message: $"Level System Settings Saved");
        }
        catch (Exception e)
        {
            Log.Error($"Failed to save level system config\n{e}");
            return await Index(id,
                messageType: "danger",
                message: $"Failed to save Level System settings. {e.Message}");
        }
    }

    [HttpPost("~/Server/{id}/Settings/Xp/RoleGrant/Remove")]
    public async Task<IActionResult> SaveSettings_Xp_RoleGrant_Remove(ulong id, string roleId)
    {
        if (!CanAccess(id))
            return View("NotAuthorized");
        
        var guild = _discord.GetGuild(id);
        if (guild == null)
            return View("NotFound", "Guild not found");

        ulong targetRoleId;
        try
        {
            targetRoleId = ulong.Parse(roleId);
        }
        catch (Exception ex)
        {
            return await Index(id,
                messageType: "danger",
                message: $"Failed to add Role Reward item. Failed to parse \"roleId\": {ex.Message}");
        }

        try
        {
            var controller = Program.Services.GetRequiredService<LevelSystemGuildConfigController>();
            var data = await controller.Get(guild.Id) ?? new LevelSystemGuildConfigModel()
            {
                GuildId = guild.Id,
            };
            var roleName = guild.Roles.FirstOrDefault(v => v.Id == targetRoleId);
            data.RoleGrant = data.RoleGrant.Where(v => v.RoleId != targetRoleId).ToList();
            await controller.Set(data);
            return await Index(
                id,
                messageType: "success",
                message: $"Removed role {roleName?.Name} to Level Up Role Reward");
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to remove item from RoleGrant (guild: {guild.Id}, role: {roleId}\n{ex}");
            return await Index(id,
                messageType: "danger",
                message: $"Failed to save Level System settings. {ex.Message}");
        }
    }


    [HttpPost("~/Server/{id}/Settings/Xp/RoleGrant/Add")]
    public async Task<IActionResult> SaveSettings_Xp_RoleGrant_Add(ulong id, string roleId, string requiredLevel)
    {
        if (!CanAccess(id))
            return View("NotAuthorized");
        
        var guild = _discord.GetGuild(id);
        if (guild == null)
            return View("NotFound", "Guild not found");

        ulong targetRoleId;
        try
        {
            targetRoleId = ulong.Parse(roleId);
        }
        catch (Exception ex)
        {
            return await Index(id,
                messageType: "danger",
                message: $"Failed to add Role Reward item. Failed to parse \"roleId\": {ex.Message}");
        }

        ulong targetRequiredLevel;
        try
        {
            targetRequiredLevel = ulong.Parse(requiredLevel);
        }
        catch (Exception ex)
        {
            return await Index(id,
                messageType: "danger",
                message: $"Failed to add Role Reward item. Failed to parse \"requiredLevel\": {ex.Message}");
        }

        try
        {
            var controller = Program.Services.GetRequiredService<LevelSystemGuildConfigController>();
            var data = await controller.Get(guild.Id) ?? new LevelSystemGuildConfigModel()
            {
                GuildId = guild.Id,
            };
            var roleName = guild.Roles.FirstOrDefault(v => v.Id == targetRoleId);
            var exists = data.RoleGrant.Any(v => v.RoleId == targetRoleId);
            if (exists)
            {
                return await Index(id,
                    messageType: "warning",
                    message: $"Role Reward for \"{roleName?.Name ?? targetRoleId.ToString()}\" already exists.");
            }
            data.RoleGrant.Add(new LevelSystemRoleGrantItem()
            {
                RoleId = targetRoleId,
                RequiredLevel = targetRequiredLevel
            });
            await controller.Set(data);
            return await Index(
                id,
                messageType: "success",
                message: $"Added role {roleName?.Name} to Level Up Role Reward");
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to add item from RoleGrant (guild: {guild.Id}, role: {roleId}, level: {requiredLevel})\n{ex}");
            return await Index(id,
                messageType: "danger",
                message: $"Failed to save Level System settings. {ex.Message}");
        }
    }
}