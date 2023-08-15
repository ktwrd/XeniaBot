using Microsoft.AspNetCore.Mvc;
using XeniaBot.Data.Controllers.BotAdditions;
using XeniaBot.Data.Models;
using XeniaBot.WebPanel.Helpers;
using XeniaBot.WebPanel.Models;

namespace XeniaBot.WebPanel.Controllers;

public partial class AdminController
{
    [HttpGet("~/Admin/Server/{id}")]
    public async Task<IActionResult> ServerInfo(ulong id)
    {
        if (!CanAccess())
            return View("NotAuthorized");

        var model = new AdminServerModel();
        await AspHelper.FillServerModel(id, model);
        
        return View("ServerInfo", model);
    }

    [HttpPost("~/Admin/Server/{id}/Setting/BanSync/State")]
    public async Task<IActionResult> SaveSettings_BanSyncState(ulong id, BanSyncGuildState state, string reason)
    {
        if (!CanAccess())
            return View("NotAuthorized");

        var controller = Program.Services.GetRequiredService<BanSyncController>();
        var model = new AdminServerModel();
        await AspHelper.FillServerModel(id, model);
        try
        {
            await controller.SetGuildState(id, state, reason);
        }
        catch (Exception e)
        {
            return RedirectToAction("Index", new
            {
                Id = id,
                MessageType = "danger",
                Message = $"Failed to set BanSync state<br/><pre><code>{e.Message}</code></pre>"
            });
        }
        return RedirectToAction("Index", new
        {
            Id = id,
            MessageType = "success",
            Message = $"BanSync: State updated to {state}"
        });
    }
}