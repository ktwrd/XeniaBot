using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using XeniaBot.WebPanel.Models.Component;

namespace XeniaBot.WebPanel.Controllers;

public partial class AdminController
{
    [HttpGet("~/Admin/Server/{id}/Settings/LevelSystem/Component")]
    public async Task<IActionResult> LevelSystemComponent(ulong id)
    {
        var model = new LevelSystemComponentViewModel();
        await model.PopulateModel(HttpContext, id);
        return PartialView("ServerInfo/LevelSystemComponent", model);
    }

    [HttpGet("~/Admin/Server/{id}/Settings/BanSyncHistory/Component")]
    [AuthRequired]
    [RequireSuperuser]
    public async Task<IActionResult> BanSyncStateHistory(ulong id)
    {
        var model = new AdminBanSyncComponentViewModel();
        await model.PopulateModel(HttpContext, id);
        return PartialView("ServerInfo/BanSyncHistoryComponent", model);
    }

    [HttpGet("~/Admin/Server/{id}/Settings/BanSync/Component")]
    [AuthRequired]
    [RequireSuperuser]
    public async Task<IActionResult> Settings_BanSyncState(ulong id)
    {
        var model = new AdminBanSyncComponentViewModel();
        await model.PopulateModel(HttpContext, id);
        return PartialView("ServerInfo/BanSyncComponent", model);
    }
}