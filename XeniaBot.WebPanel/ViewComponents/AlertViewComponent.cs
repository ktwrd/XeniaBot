using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using XeniaBot.WebPanel.Models;

namespace XeniaBot.WebPanel.ViewComponents;

public class AlertViewComponent : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(AlertComponentViewModel data)
    {
        return View("Default", data);
    }
}