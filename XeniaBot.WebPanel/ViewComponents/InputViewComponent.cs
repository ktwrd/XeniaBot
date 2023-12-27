using Microsoft.AspNetCore.Mvc;
using XeniaBot.WebPanel.Models;

namespace XeniaBot.WebPanel.ViewComponents;

public class InputViewComponent : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(InputComponentModel data)
    {
        return View("Default", data);
    }
}