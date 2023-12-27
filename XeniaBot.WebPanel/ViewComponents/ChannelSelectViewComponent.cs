using Microsoft.AspNetCore.Mvc;
using XeniaBot.WebPanel.Models;

namespace XeniaBot.WebPanel.ViewComponents;

public class ChannelSelectViewComponent : ViewComponent
{
    /// <summary>
    /// Generate whole ChannelSelect dropdown with form tags (like `name`, `id`, and `form` on the `select` element).
    ///
    /// Also contains everything in `input-group` to make it look lovely with bootstrap.
    /// </summary>
    public async Task<IViewComponentResult> InvokeAsync(ChannelSelectModel data)
    {
        return View("Default", data);
    }
}