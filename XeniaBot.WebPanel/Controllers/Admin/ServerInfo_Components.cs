using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using XeniaBot.WebPanel.Models.Component;

namespace XeniaBot.WebPanel.Controllers;

public partial class AdminController
{
    [HttpGet("~/Admin/Server/{id}/Setting/BanSync/Component")]
    [AuthRequired]
    [RequireSuperuser]
    public async Task<IActionResult> Settings_BanSyncState(ulong id)
    {
        var model = new AdminBanSyncComponentViewModel();
        await model.PopulateModel(HttpContext, id);
        return PartialView("ServerInfo/BanSyncComponent", model);
    }
}