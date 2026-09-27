using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XeniaBot.MongoData.Repositories;
using XeniaBot.Shared.Services;
using XeniaBot.WebPanel.Helpers;
using XeniaBot.WebPanel.Models;

namespace XeniaBot.WebPanel.Controllers;

[Controller]
public class ReminderController : BaseXeniaController
{
    private readonly ILogger<ReminderController> _logger;
    private readonly ReminderRepository _reminderRepo;

    public ReminderController(IServiceProvider services, ILogger<ReminderController> logger)
        : base(services)
    {
        _logger = logger;
        _reminderRepo = services.GetRequiredService<ReminderRepository>();
    }

    public async Task<ReminderViewModel> PopulateModel()
    {
        var model = new ReminderViewModel();
        await PopulateModel(model);

        var currentUserId = GetCurrentUserId();
        
        if (currentUserId != null)
        {
            model.Reminders = await _reminderRepo.GetByUser((ulong)currentUserId);
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

    [HttpGet("~/Reminders/Component")]
    [AuthRequired]
    public async Task<IActionResult> ListComponent(int cursor = 1)
    {
        var model = new ReminderListComponentViewModel();
        await model.PopulateModel(
            Services.GetRequiredService<ReminderRepository>(),
            AspHelper.GetUserId(HttpContext)!.Value,
            cursor);
        return View("ReminderListComponent", model);
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
        var dbResult = await _reminderRepo.Get(id);
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
        await _reminderRepo.Set(dbResult);
        _logger.LogInformation("Deleted reminder {ReminderId}", id);
        return RedirectToAction("Index", new Dictionary<string, object>()
        {
            {"message", "Successfully deleted reminder"},
            {"messageType", "success"}
        });
    }
}