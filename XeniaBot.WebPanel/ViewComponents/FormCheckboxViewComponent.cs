using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using XeniaBot.WebPanel.Models;

namespace XeniaBot.WebPanel.ViewComponents;

public class FormCheckboxViewComponent : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(FormCheckboxViewModel data)
    {
        return View("Default", data);
    }
}