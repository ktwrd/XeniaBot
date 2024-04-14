using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.AspNetCore.Mvc;
using XeniaBot.WebPanel.Helpers;
using XeniaBot.WebPanel.Models;
using XeniaBot.WebPanel.Models.Component;

namespace XeniaBot.WebPanel.Controllers;

public partial class AdminController
{
    [HttpGet("~/Admin")]
    [AuthRequired]
    [RequireSuperuser]
    public async Task<IActionResult> Index()
    {
        var model = new AdminIndexModel
        {
            Guilds = _client.Guilds
        };
        await PopulateModel(model);
        return View(model);
    }

    [HttpGet("~/Admin/Components/ServerList")]
    [AuthRequired]
    [RequireSuperuser]
    public async Task<IActionResult> ServerListComponent(int cursor = 1)
    {
        var model = new AdminServerListViewModel();
        model.Items = model.Paginate(
            _client.Guilds.Select(StrippedGuild.FromGuild),
            v => v.Name,
            cursor);
        return PartialView("AdminServerListComponent", model);
    }
}