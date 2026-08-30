using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.WebPanel.Extensions;
using XeniaBot.WebPanel.Models;
using XeniaDiscord.Data;

namespace XeniaBot.WebPanel.Areas.RolePreserve.Controllers;

[Controller]
public class RedirectController : Controller
{
    private readonly DiscordShardedClient _discord;
    private readonly XeniaDbContext _db;

    public RedirectController(IServiceProvider services)
    {
        _discord = services.GetRequiredService<DiscordShardedClient>();
        _db = services.GetRequiredService<XeniaDbContext>();
    }
    
    [Route("~/RolePreserve/Audit/Details")]
    public async Task<IActionResult> AuditDetailsRedirect([FromQuery] Guid id)
    {
        if (!HttpContext.IsLoggedIn())
        {
            Response.StatusCode = 401;
            return View("NotAuthorized", new NotAuthorizedViewModel()
            {
                Message = "Please log in to view this record.", 
                ShowLoginButton = true
            });
        }
        var auditRecord = await _db.RolePreserveAudit
            .Include(e => e.AppliedRoles)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);
        if (auditRecord == null)
        {
            Response.StatusCode = 404;
            return View("NotFound", new NotFoundViewModel()
            {
                Message = "Could not find Role Preserve Audit record"
            });
        }
        
        return RedirectToAction("GetDetails", "Audit", new
        {
            guildId = auditRecord.GuildId,
            id = auditRecord.Id,
            area = "RolePreserve"
        });
    }
}