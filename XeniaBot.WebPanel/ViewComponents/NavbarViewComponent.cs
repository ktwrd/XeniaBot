using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace XeniaBot.WebPanel.ViewComponents;

public class NavbarViewComponent : ViewComponent
{
    /// <summary>
    /// Only generate the option elements for a select.
    /// </summary>
    public async Task<IViewComponentResult> InvokeAsync()
    {
        return View("Default");
    }
}