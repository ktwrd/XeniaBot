using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using XeniaBot.WebPanel.Models;

namespace XeniaBot.WebPanel.ViewComponents;

public class EmbedViewerViewComponent : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(EmbedViewerModel data)
    {
        return View("Default", data);
    }
}