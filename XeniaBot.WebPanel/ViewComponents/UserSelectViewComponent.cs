using Microsoft.AspNetCore.Mvc;
using XeniaBot.WebPanel.Models;

namespace XeniaBot.WebPanel.ViewComponents;

public class UserSelectViewComponent : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(UserSelectModel data)
    {
        return View("Default", data);
    }
}