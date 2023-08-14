using Microsoft.AspNetCore.Mvc;
using XeniaBot.WebPanel.Models;

namespace XeniaBot.WebPanel.ViewComponents;

public class ChannelSelectViewComponent : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(ChannelSelectModel data)
    {
        return View("Default", data);
    }
}