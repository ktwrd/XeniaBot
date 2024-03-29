using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using XeniaBot.WebPanel.Models;

namespace XeniaBot.WebPanel.ViewComponents;

public class FormSelectViewComponent : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(FormSelectViewModel data)
    {
        return View("Default", data);
    }
}