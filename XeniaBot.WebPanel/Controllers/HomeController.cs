﻿using System.Diagnostics;
using Discord.WebSocket;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
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
        return View("Vote", await PopulateModel());
    }

    [HttpGet("~/About")]
    public async Task<IActionResult> About()
    {
        return View("About", await PopulateModel());
    }

    [AuthRequired(ShowLoginButton = true)]
    [HttpGet("~/Preferences")]
    public async Task<IActionResult> Preferences()
    {
        var data = await PopulateModel();
        return View("Preferences", data);
    }

    [AuthRequired(ShowLoginButton = true)]
    [HttpPost("~/Preferences/Save")]
    public async Task<IActionResult> PreferencesSave(ListViewStyle listViewStyle, bool enableProfileTracking)
    {
        try
        {
            var userId = (ulong)GetCurrentUserId();
            var data = await _userConfig.GetOrDefault(userId);
            data.ListViewStyle = listViewStyle;
            data.EnableProfileTracking = enableProfileTracking;
            await _userConfig.Add(data);
        }
        catch (Exception e)
        {
            Log.Error(e);
            return RedirectToAction("Preferences", new
            {
                MessageType = "danger",
                Message = $"Failed to save preferences. {e.Message}"
            });
        }
        return RedirectToAction("Preferences", new
        {
            MessageType = "success",
            Message = $"Preferences Saved"
        });
    }
}