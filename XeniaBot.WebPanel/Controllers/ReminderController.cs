using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using XeniaBot.Data.Repositories;
using XeniaBot.Shared.Services;
using XeniaBot.WebPanel.Helpers;
using XeniaBot.WebPanel.Models;

namespace XeniaBot.WebPanel.Controllers;

[Controller]
public class ReminderController : BaseXeniaController
{
    private readonly ILogger<HomeController> _logger;
    public ReminderController(ILogger<ReminderController> logger)
        : base()
    { }

    public async Task<ReminderViewModel> PopulateModel()
    {
        var model = new ReminderViewModel();
        await PopulateModel(model);

        var currentUserId = GetCurrentUserId();
        
        var db = CoreContext.Instance?.GetRequiredService<ReminderRepository>();
        if (currentUserId != null)
        {
            model.Reminders = await db.GetByUser((ulong)currentUserId);
            model.Reminders = model.Reminders.Where(v => !v.HasReminded).OrderByDescending(v => v.ReminderTimestamp)
                .ToList();
        }

        return model;
    }
    
    [HttpGet("~/Reminders")]
    [AuthRequired]
    public async Task<IActionResult> Index(string? message = null, string? messageType = null)
    {
        var model = await PopulateModel();
        
        return View("Default", model);
    }

    [HttpPost("~/Reminders/Create")]
    [AuthRequired]
    public IActionResult CreatePage()
    {
        throw new NotImplementedException();
    }

    [HttpGet("~/Reminders/{id}/Remove")]
    [AuthRequired]
    public async Task<IActionResult> Remove(string id)
    {
        var db = CoreContext.Instance?.GetRequiredService<ReminderRepository>();
        var dbResult = await db.Get(id);
        if (dbResult == null || dbResult.HasReminded)
        {
            return View("NotFound", "Reminder does not exist");
        }
        else if (dbResult.UserId != GetCurrentUserId())
        {
            return View("NotAuthorized", new NotAuthorizedViewModel()
            {
                Message = "This reminder does not belong to you"
            });
        }

        dbResult.MarkAsComplete();
        await db.Set(dbResult);
        
        return RedirectToAction("Index", new Dictionary<string, object>()
        {
            {"message", "Successfully deleted reminder"},
            {"messageType", "success"}
        });
    }
}