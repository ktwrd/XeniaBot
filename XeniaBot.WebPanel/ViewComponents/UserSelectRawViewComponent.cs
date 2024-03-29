using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using XeniaBot.WebPanel.Models;

namespace XeniaBot.WebPanel.ViewComponents;

public class UserSelectRawViewComponent : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(UserSelectModel data)
    {
        return View("Default", data);
    }
}