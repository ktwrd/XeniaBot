using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using XeniaBot.Data;
using XeniaBot.WebPanel.Extensions;
using XeniaBot.WebPanel.Helpers;
using XeniaBot.WebPanel.Models;
using Log = XeniaBot.Shared.Log;

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
        return Redirect("https://xenia.kate.pet/p/privacy_policy");
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
        return View("Vote", await PopulateModel());
    }

    [HttpGet("~/About")]
    public async Task<IActionResult> About()
    {
        return View("About", await PopulateModel());
    }

    [AuthRequired]
    [HttpGet("~/Preferences")]
    public async Task<IActionResult> Preferences()
    {
        var data = await PopulateModel();
        return View("Preferences", data);
    }

    [AuthRequired]
    [HttpPost("~/Preferences/Component")]
    public async Task<IActionResult> PreferencesComponent()
    {
        var data = await PopulateModel();
        return PartialView("Components/UserConfigComponent", data);
    }
    
    [AuthRequired]
    [HttpPost("~/Preferences/Save")]
    public async Task<IActionResult> PreferencesSave(
        ListViewStyle listViewStyle,
        bool enableProfileTracking,
        bool silentJoinMessage)
    {
        var model = await PopulateModel();
        try
        {
            // not null since AuthRequiredAttribute is used
            var userId = (ulong)GetCurrentUserId()!; 
            var data = await _userConfig.GetOrDefault(userId);
            data.ListViewStyle = listViewStyle;
            data.EnableProfileTracking = enableProfileTracking;
            data.SilentJoinMessage = silentJoinMessage;
            await _userConfig.Add(data);
            model.UserConfig = data;
        }
        catch (Exception e)
        {
            Log.Error(e);
            model.MessageType = "danger";
            model.Message = $"Failed to save preferences. {e.Message}";
            return PartialView("Components/UserConfigComponent", model);
        }
        model.MessageType = "success";
        model.Message = $"Preferences Saved";
        return PartialView("Components/UserConfigComponent", model);
    }
}