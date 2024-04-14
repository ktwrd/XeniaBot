using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using XeniaBot.Data.Models;
using XeniaBot.Data.Repositories;
using XeniaBot.WebPanel.Helpers;
using XeniaBot.WebPanel.Models.Component.FunView;

namespace XeniaBot.WebPanel.Controllers;

public partial class ServerController
{
    public async Task<ServerConfessionComponentViewModel> GetConfessionDetails(ulong serverId)
    {
        var guild = _discord.GetGuild(serverId);
        var model = new ServerConfessionComponentViewModel
        {
            Guild = guild,
            User = guild.GetUser(AspHelper.GetUserId(HttpContext) ?? 0)
        };
        
        var confessionRepo = Program.Core.GetRequiredService<ConfessionConfigRepository>();
        model.ConfessionConfig = await confessionRepo.GetGuild(model.Guild.Id) ?? new ConfessionGuildModel()
        {
            GuildId = model.Guild.Id
        };
        await PopulateModel(model);
        return model;
    }

    #region Internal Logic Handling
    private async Task<(bool, string?, object?)> InternalConfessionComponent(ulong id)
    {
        var guild = _discord.GetGuild(id);
        if (guild == null)
            return (true, "NotFound", "Guild Not Found");

        var model = await GetConfessionDetails(guild.Id);
        return (false, null, model);
    }

    private async Task<(bool, string?, object?)> InternalConfessionComponentSave(
        ulong id,
        string? modalChannelId,
        string? messageChannelId)
    {
        var componentResult = await InternalConfessionComponent(id);
        if (componentResult.Item1 == true)
        {
            return componentResult;
        }

        var model = (componentResult.Item3 as ServerConfessionComponentViewModel)!;

        if (!ParseChannelId(modalChannelId, out var modalResult))
        {
            model.MessageType = "danger";
            model.Message = $"Failed to parse ChannelId for confession modal. {modalResult.ErrorContent}";
            return (false, null, model);
        }
        if (!ParseChannelId(messageChannelId, out var channelResult))
        {
            model.MessageType = "danger";
            model.Message = $"Failed to parse ChannelId for messages. {channelResult.ErrorContent}";
            return (false, null, model);
        }
        
        var repo = Program.Core.GetRequiredService<ConfessionConfigRepository>();
        var modalId = (ulong)modalResult.ChannelId;
        var msgId = (ulong)channelResult.ChannelId;

        bool initialize = model.ConfessionConfig.ModalChannelId != modalId;
        if (initialize)
        {
            await repo.InitializeModal(id, msgId, modalId);
            model = await GetConfessionDetails(id);
        }

        model.ConfessionConfig.ModalChannelId = modalId;
        model.ConfessionConfig.ChannelId = msgId;
        await repo.Set(model.ConfessionConfig);
        
        model.MessageType = "success";
        model.Message = "Saved Settings" + (initialize ? " (Created Modal)" : "");
        return (false, null, model);
    }

    private async Task<(bool, string?, object?)> InternalConfessionComponentPurge(ulong id)
    {
        var componentResult = await InternalConfessionComponent(id);
        if (componentResult.Item1)
        {
            return componentResult;
        }

        var model = (componentResult.Item3 as ServerConfessionComponentViewModel)!;

        var repo = Program.Core.GetRequiredService<ConfessionConfigRepository>();
        await repo.Delete(model.ConfessionConfig);
        
        model.MessageType = "success";
        model.Message = "Purged all messages";
        return (false, null, model);
    }
    #endregion
    
    #region Get
    [HttpGet("~/Server/{id}/Fun/Confession")]
    [HttpGet("~/Server/{id}/Settings/Confession")]
    [AuthRequired]
    [RestrictToGuild(GuildIdRouteKey = "id")]
    public async Task<IActionResult> Settings_Confession(ulong id)
    {
        var componentResult = await InternalConfessionComponent(id);
        if (componentResult.Item1)
        {
            return View(componentResult.Item2, componentResult.Item3);
        }
        else
        {
            return View("Details/FunView/ConfessionView", componentResult.Item3);
        }
    }

    [HttpGet("~/Server/{id}/Fun/Confession/Component")]
    [HttpGet("~/Server/{id}/Settings/Confession/Component")]
    [AuthRequired]
    [RestrictToGuild(GuildIdRouteKey = "id")]
    public async Task<IActionResult> Settings_Confession_Component(ulong id)
    {
        var componentResult = await InternalConfessionComponent(id);
        if (componentResult.Item1)
        {
            return View(componentResult.Item2, componentResult.Item3);
        }
        else
        {
            return View("Details/FunView/ConfessionComponentView", componentResult.Item3);
        }
    }
    #endregion
    
    #region Save
    [HttpPost("~/Server/{id}/Settings/Confession")]
    [AuthRequired]
    [RestrictToGuild(GuildIdRouteKey = "id")]
    public async Task<IActionResult> Settings_Confession_Save(
        ulong id,
        string? modalChannelId,
        string? messageChannelId)
    {
        var componentResult = await InternalConfessionComponentSave(id, modalChannelId, messageChannelId);
        if (componentResult.Item1)
        {
            return View(componentResult.Item2, componentResult.Item3);
        }
        else
        {
            return View("Details/FunView/ConfessionView", componentResult.Item3);
        }
    }
    
    [HttpPost("~/Server/{id}/Settings/Confession/Component")]
    [AuthRequired]
    [RestrictToGuild(GuildIdRouteKey = "id")]
    public async Task<IActionResult> Settings_Confession_Save_Component(
        ulong id,
        string? modalChannelId,
        string? messageChannelId)
    {
        var componentResult = await InternalConfessionComponentSave(id, modalChannelId, messageChannelId);
        if (componentResult.Item1)
        {
            return PartialView(componentResult.Item2, componentResult.Item3);
        }
        else
        {
            return PartialView("Details/FunView/ConfessionComponentView", componentResult.Item3);
        }
    }
    #endregion

    #region Purge
    [HttpPost("~/Server/{id}/Settings/Confession/Purge")]
    [AuthRequired]
    [RestrictToGuild(GuildIdRouteKey = "id")]
    public async Task<IActionResult> Settings_Confession_Purge(ulong id)
    {
        var componentResult = await InternalConfessionComponentPurge(id);
        if (componentResult.Item1)
        {
            return View(componentResult.Item2, componentResult.Item3);
        }
        else
        {
            return View("Details/FunView/ConfessionView", componentResult.Item3);
        }
    }

    [HttpPost("~/Server/{id}/Settings/Confession/Component/Purge")]
    [AuthRequired]
    [RestrictToGuild(GuildIdRouteKey = "id")]
    public async Task<IActionResult> Settings_Confession_Purge_Component(ulong id)
    {
        var componentResult = await InternalConfessionComponentPurge(id);
        if (componentResult.Item1)
        {
            return PartialView(componentResult.Item2, componentResult.Item3);
        }
        else
        {
            return PartialView("Details/FunView/ConfessionComponentView", componentResult.Item3);
        }
    }
    #endregion
}