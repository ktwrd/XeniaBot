using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using XeniaBot.WebPanel.Models;

namespace XeniaBot.WebPanel.ViewComponents;

public class ChannelSelectRawViewComponent : ViewComponent
{
    /// <summary>
    /// Only generate the option elements for a select.
    /// </summary>
    public async Task<IViewComponentResult> InvokeAsync(ChannelSelectModel data)
    {
        return View("Default", data);
    }
}