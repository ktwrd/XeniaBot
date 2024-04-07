using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using XeniaBot.WebPanel.Models.Component;

namespace XeniaBot.WebPanel.Controllers;

public partial class AdminController
{
    [HttpGet("~/Admin/Server/{id}/Settings/LevelSystem/Component")]
    public async Task<IActionResult> LevelSystemComponent(ulong id)
    {
        var model = new AdminLevelSystemComponentViewModel();
        await model.PopulateModel(HttpContext, id);
        return PartialView("ServerInfo/LevelSystemComponent", model);
    }

    [HttpGet("~/Admin/Server/{id}/Settings/Confession/Component")]
    public async Task<IActionResult> Confession(ulong id)
    {
        var model = new AdminConfessionComponentViewModel();
        await model.PopulateModel(HttpContext, id);
        return PartialView("ServerInfo/ConfessionComponent", model);
    }

    [HttpGet("~/Admin/Server/{id}/Settings/Counting/Component")]
    public async Task<IActionResult> Counting(ulong id)
    {
        var model = new AdminCountingComponentViewModel();
        await model.PopulateModel(HttpContext, id);
        return PartialView("ServerInfo/CountingComponent", model);
    }
    
    [HttpGet("~/Admin/Server/{id}/Settings/RolePreserve/Component")]
    [AuthRequired]
    [RequireSuperuser]
    public async Task<IActionResult> RolePreserveComponent(ulong id)
    {
        var model = new AdminRolePreserveComponentViewModel();
        await model.PopulateModel(HttpContext, id);
        return PartialView("ServerInfo/RolePreserveComponent", model);
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