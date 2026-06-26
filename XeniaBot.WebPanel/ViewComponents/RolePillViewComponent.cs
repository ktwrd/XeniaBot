using System;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using XeniaBot.WebPanel.Models;

namespace XeniaBot.WebPanel.ViewComponents;

public class RolePillViewComponent : ViewComponent
{
    public Task<IViewComponentResult> InvokeAsync(RolePillComponentModel data)
    {
        try
        {
            return Task.FromResult<IViewComponentResult>(View("Default", data));
        }
        catch (Exception exception)
        {
            return Task.FromException<IViewComponentResult>(exception);
        }
    }
}