using System.Diagnostics;
using Discord.WebSocket;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using XeniaBot.WebPanel.Extensions;
using XeniaBot.WebPanel.Helpers;
using XeniaBot.WebPanel.Models;

namespace XeniaBot.WebPanel.Controllers;

[Controller]
public class HomeController : BaseXeniaController
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger) : base()
    {
        _logger = logger;
    }

    [HttpGet("~/")]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet("~/Privacy")]
    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(
            new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
    }

    [HttpGet("~/Vote")]
    public async Task<IActionResult> Vote()
    {
        return View("Vote", new DiscordModel()
        {
            Client = Program.Services.GetRequiredService<DiscordSocketClient>()
        });
    }
}