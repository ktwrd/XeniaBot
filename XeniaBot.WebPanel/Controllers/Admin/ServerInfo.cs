using Microsoft.AspNetCore.Mvc;
using XeniaBot.Data.Controllers.BotAdditions;
using XeniaBot.Data.Models;
using XeniaBot.Shared;
using XeniaBot.WebPanel.Helpers;
using XeniaBot.WebPanel.Models;

namespace XeniaBot.WebPanel.Controllers;

public partial class AdminController
{
    [HttpGet("~/Admin/Server/{id}")]
    public async Task<IActionResult> ServerInfo(ulong id, string? message, string? messageType)
    {
        if (!CanAccess())
            return View("NotAuthorized");

        var model = new AdminServerModel();
        await AspHelper.FillServerModel(id, model);
        model.Message = message;
        model.MessageType = messageType;
        await PopulateModel(model);
        return View("ServerInfo", model);
    }

    [HttpPost("~/Admin/Server/{id}/Setting/BanSync/State")]
    public async Task<IActionResult> SaveSettings_BanSyncState(ulong id, BanSyncGuildState state, string reason)
    {
        if (!CanAccess())
            return View("NotAuthorized");

        var controller = Program.Core.GetRequiredService<BanSyncController>();
        var model = new AdminServerModel();
        await AspHelper.FillServerModel(id, model);
        await PopulateModel(model);
        try
        {
            var res = await controller.SetGuildState(id, state, reason);
            if (res == null)
                throw new Exception("Server Config not found");
        }
        catch (Exception e)
        {
            Log.Error(e);
            return RedirectToAction("ServerInfo", new
            {
                Id = id,
                MessageType = "danger",
                Message = $"Failed to set BanSync state. {e.Message}"
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