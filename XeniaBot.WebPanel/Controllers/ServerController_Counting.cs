using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using XeniaBot.Data.Models;
using XeniaBot.Data.Repositories;
using XeniaBot.WebPanel.Helpers;
using XeniaBot.WebPanel.Models.Component.FunView;

namespace XeniaBot.WebPanel.Controllers;

public partial class ServerController
{
    public async Task<ServerCountingComponentViewModel> GetCountingDetails(ulong serverId)
    {
        var guild = _discord.GetGuild(serverId);
        var model = new ServerCountingComponentViewModel
        {
            Guild = guild,
            User = guild.GetUser(AspHelper.GetUserId(HttpContext) ?? 0)
        };
        
        var countingRepo = Program.Core.GetRequiredService<CounterConfigRepository>();
        model.CounterConfig = await countingRepo.Get(model.Guild) ?? new CounterGuildModel()
        {
            GuildId = model.Guild.Id
        };
        await PopulateModel(model);
        return model;
    }
    
    #region Internal Logic Handling
    private async Task<(bool, string?, object?)> InternalCountingComponent(ulong id)
    {
        var guild = _discord.GetGuild(id);
        if (guild == null)
            return (true, "NotFound", "Guild Not Found");
        
        var model = await GetCountingDetails(guild.Id);
        return (false, null, model);
    }

    private async Task<(bool, string?, object?)> InternalCountingComponentSave(
        ulong id,
        string? inputChannelId)
    {
        var componentResult = await InternalCountingComponent(id);
        if (componentResult.Item1 == true)
        {
            return componentResult;
        }
        
        var model = (componentResult.Item3 as ServerCountingComponentViewModel)!;

        inputChannelId ??= "0";
        if (!ParseChannelId(inputChannelId, out var parsedChannel))
        {
            model.MessageType = "danger";
            model.Message = $"Failed to parse ChannelId. {parsedChannel.ErrorContent}";
            return (false, null, model);
        }

        var repo = Program.Core.GetRequiredService<CounterConfigRepository>();
        model.CounterConfig.ChannelId = parsedChannel.ChannelId;
        await repo.Set(model.CounterConfig);
        
        model.MessageType = "success";
        model.Message = "Saved settings.";
        return (false, null, model);
    }
    #endregion

    #region Get
    [HttpGet("~/Server/{id}/Fun/Counting")]
    [HttpGet("~/Server/{id}/Settings/Counting")]
    [AuthRequired]
    [RestrictToGuild(GuildIdRouteKey = "id")]
    public async Task<IActionResult> Settings_Counting(ulong id)
    {
        var componentResult = await InternalCountingComponent(id);
        if (componentResult.Item1)
        {
            return View(componentResult.Item2, componentResult.Item3);
        }
        else
        {
            return View("Details/FunView/CountingView", componentResult.Item3);
        }
    }

    [HttpGet("~/Server/{id}/Fun/Counting/Component")]
    [HttpGet("~/Server/{id}/Settings/Counting/Component")]
    [AuthRequired]
    [RestrictToGuild(GuildIdRouteKey = "id")]
    public async Task<IActionResult> Settings_Counting_Component(ulong id)
    {
        var componentResult = await InternalCountingComponent(id);
        if (componentResult.Item1)
        {
            return PartialView(componentResult.Item2, componentResult.Item3);
        }
        else
        {
            return PartialView("Details/FunView/CountingComponentView", componentResult.Item3);
        }
    }
    #endregion
    
    #region Save
    [HttpPost("~/Server/{id}/Settings/Counting")]
    [AuthRequired]
    [RestrictToGuild(GuildIdRouteKey = "id")]
    public async Task<IActionResult> Settings_Counting_Save(
        ulong id,
        string? inputChannelId)
    {
        var componentResult = await InternalCountingComponentSave(id, inputChannelId);
        if (componentResult.Item1)
        {
            return View(componentResult.Item2, componentResult.Item3);
        }
        else
        {
            return View("Details/FunView/CountingView", componentResult.Item3);
        }
    }

    [HttpPost("~/Server/{id}/Settings/Counting/Component")]
    [AuthRequired]
    [RestrictToGuild(GuildIdRouteKey = "id")]
    public async Task<IActionResult> Settings_Counting_Save_Component(
        ulong id,
        string? inputChannelId)
    {
        var componentResult = await InternalCountingComponentSave(id, inputChannelId);
        if (componentResult.Item1)
        {
            return PartialView(componentResult.Item2, componentResult.Item3);
        }
        else
        {
            return PartialView("Details/FunView/CountingComponentView", componentResult.Item3);
        }
    }
    #endregion
}