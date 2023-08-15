using Microsoft.AspNetCore.Mvc;
using XeniaBot.WebPanel.Models;

namespace XeniaBot.WebPanel.Controllers;

public partial class AdminController
{
    [HttpGet("~/Admin")]
    public IActionResult Index()
    {
        if (!CanAccess())
            return View("NotAuthorized");
        var model = new AdminIndexModel();
        model.Guilds = _client.Guilds;
        return View(model);
    }
}