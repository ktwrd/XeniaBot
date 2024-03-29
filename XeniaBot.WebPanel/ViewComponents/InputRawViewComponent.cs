using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using XeniaBot.WebPanel.Models;

namespace XeniaBot.WebPanel.ViewComponents;

public class InputRawViewComponent : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(InputComponentModel data)
    {
        return View("Default", data);
    }
}