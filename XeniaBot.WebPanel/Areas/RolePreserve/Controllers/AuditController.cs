using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Shared.Helpers;
using XeniaBot.WebPanel.Areas.RolePreserve.Models.Audit;
using XeniaBot.WebPanel.Extensions;
using XeniaBot.WebPanel.Models;
using XeniaDiscord.Data;
using XeniaDiscord.Data.Models.Snapshot;

namespace XeniaBot.WebPanel.Areas.RolePreserve.Controllers;

[Area("RolePreserve")]
[Route("~/[area]/[controller]")]
[AuthRequired]
[Authorize]
public class AuditController : Controller
{
    private readonly DiscordSocketClient _discord;
    private readonly XeniaDbContext _db;

    public AuditController(IServiceProvider services)
    {
        _discord = services.GetRequiredService<DiscordSocketClient>();
        _db = services.GetRequiredService<XeniaDbContext>();
    }

    [HttpGet("Details")]
    public async Task<IActionResult> GetDetails(Guid id)
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
        var currentMember = await HttpContext.GetCurrentDiscordGuildMember(auditRecord.GetGuildId());
        if (currentMember == null || !currentMember.GuildPermissions.Has(GuildPermission.ViewAuditLog))
        {
            Response.StatusCode = 401;
            return View("NotAuthorized", new NotAuthorizedViewModel()
            {
                Message = "You do not have permission to view this record. You are either not a member of the server this record belongs to, or you're missing the permission \"View Audit Log\" in the server."
            });
        }

        var guild = await ExceptionHelper.RetryOnTimedOut(async () => _discord.GetGuild(auditRecord.GetGuildId()));
        var guildSnapshot = await _db.GuildSnapshots.AsNoTracking()
            .Where(e => e.GuildId == auditRecord.GuildId)
            .OrderByDescending(e => e.RecordCreatedAt)
            .FirstOrDefaultAsync();

        IUser? user = null;
        UserSnapshotModel? userSnapshot = null;

        IUser? targetUser = null;
        UserSnapshotModel? targetUserSnapshot = null;
        
        var userId = auditRecord.GetUserId();
        var targetUserId = auditRecord.GetTargetUserId();

        if (userId.HasValue)
        {
            user = await ExceptionHelper.RetryOnTimedOut(async () => _discord.GetUser(userId.Value));
            userSnapshot = await _db.UserSnapshots.AsNoTracking()
                .Where(e => e.UserId == auditRecord.UserId)
                .OrderByDescending(e => e.RecordCreatedAt)
                .FirstOrDefaultAsync();
        }

        if (targetUserId.HasValue)
        {
            targetUser = await ExceptionHelper.RetryOnTimedOut(async () => _discord.GetUser(targetUserId.Value));
            targetUserSnapshot = await _db.UserSnapshots.AsNoTracking()
                .Where(e => e.UserId == auditRecord.TargetUserId)
                .OrderByDescending(e => e.RecordCreatedAt)
                .FirstOrDefaultAsync();
        }


        var vm = new DetailsViewModel()
        {
            Record = auditRecord,
            Guild = guild,
            GuildSnapshot = guildSnapshot,
            User = user,
            UserSnapshot = userSnapshot,
            TargetUser = targetUser,
            TargetUserSnapshot = targetUserSnapshot,
            GuildId = auditRecord.GetGuildId()
        };

        return View("Details", vm);
    }
}
