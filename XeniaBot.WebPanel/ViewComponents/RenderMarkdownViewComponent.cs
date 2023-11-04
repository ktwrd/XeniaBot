using Microsoft.AspNetCore.Mvc;
using XeniaBot.WebPanel.Models;

namespace XeniaBot.WebPanel.ViewComponents;

public class RenderMarkdownViewComponent : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(string data)
    {
        return View("Default", new RenderMarkdownViewModel()
        {
            Content = data
        });
    }
}