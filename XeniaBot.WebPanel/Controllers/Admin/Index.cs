using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using XeniaBot.WebPanel.Models;

namespace XeniaBot.WebPanel.Controllers;

public partial class AdminController
{
    [HttpGet("~/Admin")]
    [AuthRequired(RequireWhitelist = true)]
    public async Task<IActionResult> Index()
    {
        var model = new AdminIndexModel
        {
            Guilds = _client.Guilds
        };
        await PopulateModel(model);
        return View(model);
    }
}