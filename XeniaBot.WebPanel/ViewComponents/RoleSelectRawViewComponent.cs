using Microsoft.AspNetCore.Mvc;
using XeniaBot.WebPanel.Models;

namespace XeniaBot.WebPanel.ViewComponents;

public class RoleSelectRawViewComponent : ViewComponent
{
    /// <summary>
    /// Only generate the `select` elements for <see cref="RoleSelectViewComponent"/>
    /// </summary>
    public async Task<IViewComponentResult> InvokeAsync(RoleSelectRawComponentModel data)
    {
        return View("Default", data);
    }
}