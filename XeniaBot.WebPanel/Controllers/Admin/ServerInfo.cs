using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using XeniaBot.Data.Repositories;
using XeniaBot.Data.Models;
using XeniaBot.Data.Services;
using XeniaBot.Shared;
using XeniaBot.Shared.Services;
using XeniaBot.WebPanel.Helpers;
using XeniaBot.WebPanel.Models;
using XeniaBot.WebPanel.Models.Component;

namespace XeniaBot.WebPanel.Controllers;

public partial class AdminController
{
    /// <summary>
    /// Get information about a Guild
    /// </summary>
    /// <param name="id">Guild Id</param>
    /// <param name="message">Banner message</param>
    /// <param name="messageType">Banner message type</param>
    [HttpGet("~/Admin/Server/{id}")]
    [AuthRequired]
    [RequireSuperuser]
    public async Task<IActionResult> ServerInfo(ulong id, string? message, string? messageType)
    {
        var model = new AdminServerModel();
        await AspHelper.FillServerModel(id, model);
        model.Message = message;
        model.MessageType = messageType;
        await PopulateModel(model);
        return View("ServerInfo", model);
    }

    /// <summary>
    /// Refresh stored bans in the Guild provided
    /// </summary>
    /// <param name="id">Guild Id</param>
    [HttpGet("~/Admin/Server/{id}/Settings/BanSync/Refresh")]
    [AuthRequired]
    [RequireSuperuser]
    public async Task<IActionResult> BanSync_Refresh(ulong id)
    {
        var model = new AdminBanSyncComponentViewModel();
        await model.PopulateModel(HttpContext, id);
        try
        {
            var controller = Program.Core.GetRequiredService<BanSyncService>();
            await controller.RefreshBans(id);
            model.MessageType = "success";
            model.Message = "Refreshed Ban Records";
            return PartialView("ServerInfo/BanSyncComponent", model);
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            await Program.Core.GetRequiredService<ErrorReportService>()
                .ReportException(ex, $"Failed to refresh bans in {id}");
            model.MessageType = "danger";
            model.Message = $"Failed to refresh. {ex.Message}";
            return PartialView("ServerInfo/BanSyncComponent", model);
        }
    }
    
    /// <summary>
    /// Save BanSync State
    /// </summary>
    /// <param name="id">Guild Id</param>
    /// <param name="state">State</param>
    /// <param name="reason">Reason</param>
    [HttpPost("~/Admin/Server/{id}/Settings/BanSync/State")]
    [AuthRequired]
    [RequireSuperuser]
    public async Task<IActionResult> SaveSettings_BanSyncState(ulong id, BanSyncGuildState state, string reason)
    {
        var controller = Program.Core.GetRequiredService<BanSyncService>();
        var model = new AdminBanSyncComponentViewModel();
        await model.PopulateModel(HttpContext, id);
        try
        {
            var res = await controller.SetGuildState(id, state, reason);
            if (res == null)
                throw new Exception("Server Config not found");
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            model.MessageType = "danger";
            model.Message = ex.Message;
            return PartialView("ServerInfo/BanSyncComponent", model);
        }
        model.MessageType = "success";
        model.Message = $"Updated state to {state}";
        return PartialView("ServerInfo/BanSyncComponent", model);
    }
}