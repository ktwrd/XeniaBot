﻿using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using XeniaBot.Data.Repositories;
using XeniaBot.Data.Models;
using XeniaBot.Data.Services;
using XeniaBot.Shared;
using XeniaBot.Shared.Services;
using XeniaBot.WebPanel.Helpers;
using XeniaBot.WebPanel.Models;

namespace XeniaBot.WebPanel.Controllers;

public partial class AdminController
{
    [HttpGet("~/Admin/Server/{id}")]
    [AuthRequired(RequireWhitelist = true)]
    public async Task<IActionResult> ServerInfo(ulong id, string? message, string? messageType)
    {
        var model = new AdminServerModel();
        await AspHelper.FillServerModel(id, model);
        model.Message = message;
        model.MessageType = messageType;
        await PopulateModel(model);
        return View("ServerInfo", model);
    }

    [HttpGet("~/Admin/Server/{id}/Setting/BanSync/Refresh")]
    [AuthRequired(RequireWhitelist = true)]
    public async Task<IActionResult> BanSync_Refresh(ulong id)
    {
        try
        {
            var controller = Program.Core.GetRequiredService<BanSyncService>();
            await controller.RefreshBans(id);
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            await Program.Core.GetRequiredService<ErrorReportService>()
                .ReportException(ex, $"Failed to refresh bans in {id}");
            return RedirectToAction("ServerInfo", new
            {
                Id = id,
                MessageType = "danger",
                Message = $"Failed to refresh BanSync records. {ex.Message}"
            });
        }
        return RedirectToAction("ServerInfo", new
        {
            Id = id,
            MessageType = "success",
            Message = $"Refreshed BanSync records."
        });
    }
    
    [HttpPost("~/Admin/Server/{id}/Setting/BanSync/State")]
    [AuthRequired(RequireWhitelist = true)]
    public async Task<IActionResult> SaveSettings_BanSyncState(ulong id, BanSyncGuildState state, string reason)
    {
        var controller = Program.Core.GetRequiredService<BanSyncService>();
        var model = new AdminServerModel();
        await AspHelper.FillServerModel(id, model);
        await PopulateModel(model);
        try
        {
            var res = await controller.SetGuildState(id, state, reason);
            if (res == null)
                throw new Exception("Server Config not found");
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            return RedirectToAction("ServerInfo", new
            {
                Id = id,
                MessageType = "danger",
                Message = $"Failed to set BanSync state. {ex.Message}"
            });
        }
        return RedirectToAction("ServerInfo", new
        {
            Id = id,
            MessageType = "success",
            Message = $"BanSync: State updated to {state}"
        });
    }
}