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
    public IActionResult ServerListComponent(int cursor = 1)
    {
        var model = new AdminServerListViewModel
        {
            Guilds = PaginateGuild(_client.Guilds, cursor, 3),
            Cursor = cursor
        };
        return PartialView("ServerListComponent", model);
    }


    public List<SocketGuild> PaginateGuild(IEnumerable<SocketGuild> data, int page, int pageSize = 10)
    {
        return AspHelper.Paginate<SocketGuild, string>(data, v => v.Name, page, pageSize);
    }
}