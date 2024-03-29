using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using XeniaBot.WebPanel.Models;

namespace XeniaBot.WebPanel.Controllers;

public partial class AdminController
{
    [HttpGet("~/Admin")]
    public async Task<IActionResult> Index()
    {
        if (!CanAccess())
            return View("NotAuthorized");
        var model = new AdminIndexModel
        {
            Guilds = _client.Guilds
        };
        await PopulateModel(model);
        return View(model);
    }
}