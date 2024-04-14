using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using XeniaBot.Data.Models;
using XeniaBot.Data.Repositories;
using XeniaBot.WebPanel.Helpers;
using XeniaBot.WebPanel.Models.Component.FunView;

namespace XeniaBot.WebPanel.Controllers;

public partial class ServerController
{
    public async Task<ServerLevelSystemComponentViewModel> GetLevelSystemDetails(ulong serverId)
    {
        var guild = _discord.GetGuild(serverId);
        var model = new ServerLevelSystemComponentViewModel
        {
            Guild = guild,
            User = guild.GetUser(AspHelper.GetUserId(HttpContext) ?? 0)
        };
        
        var repo = Program.Core.GetRequiredService<LevelSystemConfigRepository>();
        model.LevelSystemConfig = await repo.Get(guild.Id) ?? new LevelSystemConfigModel()
        {
            GuildId = guild.Id
        };
        
        await PopulateModel(model);
        return model;
    }

    private async Task<(bool, string?, object?)> InternalLevelSystemComponent(ulong id)
    {
        var guild = _discord.GetGuild(id);
        if (guild == null)
            return (true, "NotFound", "Guild Not Found");

        var model = await GetLevelSystemDetails(guild.Id);
        return (false, null, model);
    }

    private async Task<(bool, string?, object?)> InternalLevelSystemComponentAdd(
        ulong id,
        string? roleId,
        string? requiredLevel)
    {
        var componentResult = await InternalLevelSystemComponent(id);
        if (componentResult.Item1 == true)
        {
            return componentResult;
        }
        
        var model = (componentResult.Item3 as ServerLevelSystemComponentViewModel)!;

        // parse value
        if (!ParseUlong(roleId, out var roleIdResult))
        {
            model.MessageType = "danger";
            model.Message = $"Failed to parse Role Id. {roleIdResult.ErrorContent}";
            return (false, null, model);
        }
        if (!ParseUlong(requiredLevel, out var targetLevelResult))
        {
            model.MessageType = "danger";
            model.Message = $"Failed to parse Target Level. {targetLevelResult.ErrorContent}";
            return (false, null, model);
        }

        var targetRoleId = roleIdResult.Value;
        var targetRequiredLevel = targetLevelResult.Value;

        var repo = Program.Core.GetRequiredService<LevelSystemConfigRepository>();
        
        // only add if it doesn't exist
        var roleName = model.Guild.Roles.FirstOrDefault(v => v.Id == targetRoleId);
        var exists = model.LevelSystemConfig.RoleGrant.Any(v => v.RoleId == targetRoleId);
        if (exists)
        {
            model.MessageType = "warning";
            model.Message = $"Role Reward for \"{roleName?.Name ?? targetRoleId.ToString()}\" already exists.";
            return (false, null, model);
        }
        
        model.LevelSystemConfig.RoleGrant.Add(new LevelSystemRoleGrantItem()
        {
            RoleId = targetRoleId,
            RequiredLevel = targetRequiredLevel
        });
        await repo.Set(model.LevelSystemConfig);
        model.MessageType = "success";
        model.Message = $"Added role {roleName?.Name} to rewards";
        return (false, null, model);
    }

    private async Task<(bool, string?, object?)> InternalLevelSystemComponentRemove(
        ulong id,
        string? roleId)
    {
        var componentResult = await InternalLevelSystemComponent(id);
        if (componentResult.Item1 == true)
        {
            return componentResult;
        }
        
        var model = (componentResult.Item3 as ServerLevelSystemComponentViewModel)!;

        // parse value
        if (!ParseUlong(roleId, out var roleIdResult))
        {
            model.MessageType = "danger";
            model.Message = $"Failed to parse Role Id. {roleIdResult.ErrorContent}";
            return (false, null, model);
        }

        var targetRoleId = roleIdResult.Value;

        var repo = Program.Core.GetRequiredService<LevelSystemConfigRepository>();
        
        var roleName = model.Guild.Roles.FirstOrDefault(v => v.Id == targetRoleId);
        model.LevelSystemConfig.RoleGrant = model.LevelSystemConfig.RoleGrant.Where(v => v.RoleId != targetRoleId).ToList();
        await repo.Set(model.LevelSystemConfig);

        model.MessageType = "success";
        model.Message = $"Removed role {roleName?.Name} from rewards";
        return (false, null, model);
    }

    private async Task<(bool, string?, object?)> InternalLevelSystemComponentSave(
        ulong id,
        string? channelId,
        bool show,
        bool enable)
    {
        var componentResult = await InternalLevelSystemComponent(id);
        if (componentResult.Item1 == true)
        {
            return componentResult;
        }
        
        var model = (componentResult.Item3 as ServerLevelSystemComponentViewModel)!;

        channelId ??= "0";
        // parse value
        if (!ParseUlong(channelId, out var channelIdResult))
        {
            model.MessageType = "danger";
            model.Message = $"Failed to parse Channel Id. {channelIdResult.ErrorContent}";
            return (false, null, model);
        }
        
        var repo = Program.Core.GetRequiredService<LevelSystemConfigRepository>();

        var targetChannelId = channelIdResult.Value;
        
        model.LevelSystemConfig.LevelUpChannel = targetChannelId;
        model.LevelSystemConfig.ShowLeveUpMessage = show;
        model.LevelSystemConfig.Enable = enable;
        await repo.Set(model.LevelSystemConfig);

        model.MessageType = "success";
        model.Message = $"Saved Settings";
        return (false, null, model);
    }
    
    #region Get
    [HttpGet("~/Server/{id}/Fun/LevelSystem")]
    [HttpGet("~/Server/{id}/Settings/LevelSystem")]
    [AuthRequired]
    [RestrictToGuild(GuildIdRouteKey = "id")]
    public async Task<IActionResult> Settings_LevelSystem(ulong id)
    {
        var result = await InternalLevelSystemComponent(id);
        if (result.Item1)
        {
            return View(result.Item2, result.Item3);
        }
        else
        {
            return View("Details/FunView/LevelSystemView", result.Item3);
        }
    }

    [HttpGet("~/Server/{id}/Fun/LevelSystem/General/Component")]
    [HttpGet("~/Server/{id}/Settings/LevelSystem/General/Component")]
    [AuthRequired]
    [RestrictToGuild(GuildIdRouteKey = "id")]
    public async Task<IActionResult> Settings_LevelSystem_General_Component(ulong id)
    {
        var componentResult = await InternalLevelSystemComponent(id);
        if (componentResult.Item1)
        {
            return PartialView(componentResult.Item2, componentResult.Item3);
        }
        else
        {
            return PartialView("Details/FunView/LevelSystem/GeneralComponentView", componentResult.Item3);
        }
    }

    [HttpGet("~/Server/{id}/Fun/LevelSystem/Reward/Component")]
    [HttpGet("~/Server/{id}/Settings/LevelSystem/Reward/Component")]
    [AuthRequired]
    [RestrictToGuild(GuildIdRouteKey = "id")]
    public async Task<IActionResult> Settings_LevelSystem_Reward_Component(ulong id)
    {
        var componentResult = await InternalLevelSystemComponent(id);
        if (componentResult.Item1)
        {
            return PartialView(componentResult.Item2, componentResult.Item3);
        }
        else
        {
            return PartialView("Details/FunView/LevelSystem/RewardComponentView", componentResult.Item3);
        }
    }
    #endregion
    
    #region Save Config
    [HttpPost("~/Server/{id}/Settings/LevelSystem/General/Component")]
    [AuthRequired]
    [RestrictToGuild(GuildIdRouteKey = "id")]
    public async Task<IActionResult> Settings_LevelSystem_Save_Component(
        ulong id,
        string? channelId,
        bool show,
        bool enable)
    {
        var result = await InternalLevelSystemComponentSave(id, channelId, show, enable);
        if (result.Item1)
        {
            return PartialView(result.Item2, result.Item3);
        }
        else
        {
            return PartialView("Details/FunView/LevelSystem/GeneralComponentView", result.Item3);
        }
    }
    #endregion
    
    #region Reward Add/Remove
    [HttpPost("~/Server/{id}/Settings/LevelSystem/Reward/Add/Component")]
    [AuthRequired]
    [RestrictToGuild(GuildIdRouteKey = "id")]
    public async Task<IActionResult> Settings_LevelSystem_Reward_Add_Component(
        ulong id,
        string? roleId,
        string? requiredLevel)
    {
        var result = await InternalLevelSystemComponentAdd(id, roleId, requiredLevel);
        if (result.Item1)
        {
            return PartialView(result.Item2, result.Item3);
        }
        else
        {
            return PartialView("Details/FunView/LevelSystem/RewardComponentView", result.Item3);
        }
    }

    [HttpPost("~/Server/{id}/Settings/LevelSystem/Reward/Remove/Component")]
    [AuthRequired]
    [RestrictToGuild(GuildIdRouteKey = "id")]
    public async Task<IActionResult> Settings_LevelSystem_Reward_Remove_Component(
        ulong id,
        string? roleId)
    {
        var result = await InternalLevelSystemComponentRemove(id, roleId);
        if (result.Item1)
        {
            return PartialView(result.Item2, result.Item3);
        }
        else
        {
            return PartialView("Details/FunView/LevelSystem/RewardComponentView", result.Item3);
        }
    }
    #endregion
}