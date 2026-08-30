using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Shared.Helpers;
using XeniaBot.Shared.Services;
using XeniaBot.WebPanel.Areas.RolePreserve.Models.Settings;
using XeniaBot.WebPanel.Extensions;
using XeniaBot.WebPanel.Models;
using XeniaDiscord.Common.Helpers;
using XeniaDiscord.Common.Services;
using XeniaDiscord.Data;
using XeniaDiscord.Data.Models.RolePreserve;
using XeniaDiscord.Data.Models.Snapshot;
using XeniaDiscord.Data.Repositories;

namespace XeniaBot.WebPanel.Areas.RolePreserve.Controllers;

[Authorize]
[AuthRequired]
[Area("RolePreserve")]
[Route("~/Guild/{guildId}/[area]/[controller]")]
[RestrictToGuild(GuildIdRouteKey = "guildId")]
public class SettingsController : Controller
{
    private readonly GuildCacheRepository _guildCacheRepo;
    private readonly DiscordSnapshotService _discordSnapshotService;
    private readonly RolePreserveGuildRepository _rolePreserveGuildRepo;
    private readonly DiscordShardedClient _client;
    private readonly ErrorReportService _err;
    private readonly XeniaDbContext _db;
    private readonly IDbContextFactory<XeniaDbContext> _dbContextFactory;

    public SettingsController(IServiceProvider services)
    {
        _guildCacheRepo = services.GetRequiredService<GuildCacheRepository>();
        _discordSnapshotService = services.GetRequiredService<DiscordSnapshotService>();
        _rolePreserveGuildRepo = services.GetRequiredService<RolePreserveGuildRepository>();
        _client = services.GetRequiredService<DiscordShardedClient>();
        _err = services.GetRequiredService<ErrorReportService>();
        _db = services.GetRequiredService<XeniaDbContext>();
        _dbContextFactory = services.GetRequiredService<IDbContextFactory<XeniaDbContext>>();
    }

    [Route("", Name = "Guild_RolePreserve_Settings_Index")]
    public async Task<IActionResult> Index(ulong guildId)
    {
        var vm = await GetViewModel(guildId);
        return View("Default", vm);
    }

    private async Task<DetailsViewModel> GetViewModel(ulong guildId)
    {
        var roles = await GetAvailableRoles(guildId);
        var rolePreserveGuild = await GetRolePreserveGuild(guildId);
        var roleBlacklist = await GetRoleBlacklist(guildId);
        var strippedGuild = await GetStrippedGuild(guildId);
        // TODO for role preserve logging - var logChannel = await GetStrippedLogChannel(guildId);
        // TODO for role preserve logging - var availableChannels = await GetAvailableChannels(guildId);
        var vm = new DetailsViewModel()
        {
            Enable = rolePreserveGuild?.Enabled == true,
            RoleBlacklist = roleBlacklist,
            Guild = strippedGuild,
            AvailableRoles = roles,
            /* TODO for role preserve logging
            LogChannel = logChannel,
            AvailableChannels = availableChannels
            */
        };
        return vm;
    }

    [HttpPost("_ComponentSave", Name = "Guild_RolePreserve_Settings_ComponentSave")]
    public async Task<IActionResult> SaveComponent(
        ulong guildId,
        [FromForm] bool rolePreserveEnable,
        [FromForm] string[] rolePreserveBlacklistedRoles,
        [FromForm] bool rolePreserveEnableLogging,
        [FromForm] bool rolePreserveLogChannelFromFallback,
        [FromForm] string? rolePreserveLogChannel)
    {
        var blacklistedRoleIds = rolePreserveBlacklistedRoles
            .Select(e => e.ParseULong().GetValueOrDefault(0))
            .Where(e => e > 0)
            .ToArray();
        var logOpt = SaveRolePreserveLogCfgKind.Disabled;
        if (rolePreserveEnableLogging)
            logOpt = rolePreserveLogChannelFromFallback
                ? SaveRolePreserveLogCfgKind.Fallback
                : SaveRolePreserveLogCfgKind.Custom;

        var saveOptions = new PerformSaveOptions(
            rolePreserveEnable,
            blacklistedRoleIds,
            logOpt,
            rolePreserveLogChannel.ParseULong());
        var saveResult = await PerformSave(guildId, saveOptions);

        var vm = await GetViewModel(guildId);
        if (saveResult.IsSuccess)
        {
            vm.Alert = new AlertComponentViewModel()
            {
                MessageType = "success",
                Message = "Successfully saved settings",
                ShowClose = true
            };
        }
        else
        {
            var content = string.Join("\n",
                saveResult.Errors
                    .Select(e => (string.IsNullOrEmpty(e.Name) ? "" : $"- **{e.Name}:** ") + e.Text));
            vm.Alert = new AlertComponentViewModel()
            {
                MessageType = "danger",
                Message = $"Failed to save settings:\n{content}",
                RenderMessageAsMarkdown = true
            };
        }

        return PartialView("Default", vm);
    }

    private async Task<PerformSaveResult> PerformSave(
        ulong guildId, 
        PerformSaveOptions options)
    {
        var requestingUser = await HttpContext.GetCurrentDiscordUser();
        var guildIdStr = guildId.ToString();
        var guild = ExceptionHelper.RetryOnTimedOut(() => _client.GetGuild(guildId));
        var errors = new List<BasicErrorItem>();
        await PerformSaveValidateLogChannel(guildId, options, errors);

        if (errors.Count > 0)
        {
            return new PerformSaveResult
            {
                Errors = errors.ToArray()
            };
        }

        var availableRoleIdStrs = await _db.GuildRoleSnapshots
            .Where(e => e.GuildId == guildIdStr)
            .Select(e => e.RoleId)
            .Distinct()
            .ToArrayAsync();
        ulong[] discordRoleIds = [];
        if (guild != null)
            discordRoleIds = guild.Roles.Select(e => e.Id).ToArray();
        var availableRoleIds = availableRoleIdStrs.Select(e => e.ParseULong(false).GetValueOrDefault(0))
            .Concat(discordRoleIds)
            .Where(e => e > 0)
            .Distinct()
            .ToArray();

        var existingBlacklistedRoleIdStrs = await _db.RolePreserveBlacklistedRoles
            .Where(e => e.GuildId == guildIdStr)
            .Select(e => e.RoleId)
            .Distinct()
            .ToArrayAsync();
        var existingBlacklistedRoleIds = existingBlacklistedRoleIdStrs
            .Select(e => e.ParseULong(false).GetValueOrDefault(0))
            .Where(e => e > 0)
            .Distinct()
            .ToArray();
        var blacklistedRoleIds = options.RoleBlacklist
            .Where(e => e > 0 && availableRoleIds.Contains(e))
            .ToArray();
        
        var blacklistedRoleIdsToRemove = existingBlacklistedRoleIds
            .Where(e => !blacklistedRoleIds.Contains(e)).Distinct()
            .ToArray();
        var blacklistedRoleIdsToAdd = blacklistedRoleIds
            .Where(e => !existingBlacklistedRoleIds.Contains(e)).Distinct()
            .ToArray();


        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var guildLastUpdated = await _guildCacheRepo.LastUpdated(db, guildId);
        var shouldUpdateCache = DateTime.UtcNow - guildLastUpdated.GetValueOrDefault(DateTime.MinValue)
                           > TimeSpan.FromDays(7);
        var now = DateTime.UtcNow;
        await using var trans = await db.Database.BeginTransactionAsync();
        try
        {
            // refresh cache/snapshot for guild + roles
            if (shouldUpdateCache && guild != null)
            {
                await _discordSnapshotService.UpdateGuild(
                    db, guild, now, DiscordSnapshotSource.Unknown,
                    skipRoles: false, skipMembers: true);
            }

            if (blacklistedRoleIdsToRemove.Length > 0)
            {
                await _rolePreserveGuildRepo.RoleBlacklistRemoveRange(
                    db,
                    guildId,
                    blacklistedRoleIdsToRemove,
                    doneByUser: requestingUser,
                    now: now);
            }

            if (blacklistedRoleIdsToAdd.Length > 0)
            {
                await _rolePreserveGuildRepo.RoleBlacklistAddRange(
                    db,
                    guildId,
                    blacklistedRoleIdsToAdd,
                    doneByUser: requestingUser,
                    now: now);
            }

            await _rolePreserveGuildRepo.EnableAsync(db, guildId, options.Enable, requestingUser);
            // TODO for role preserve logging - TODO send message in role preserve log channel saying what stuff has changed (it's audited anyways)

            await db.SaveChangesAsync();
            await trans.CommitAsync();
        }
        catch (Exception ex)
        {
            await trans.RollbackAsync();
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes(
                    $"User {requestingUser?.Username} failed to update Role Preserve Settings for Guild {guild?.Name} (userId={requestingUser?.Id}, guildId={guildId})")
                .AddSerializedAttachment("options.json", options));
            await trans.RollbackAsync();
            return new PerformSaveResult
            {
                IsSuccess = false,
                Errors = [
                    new BasicErrorItem("Fatal", "Failed to save settings")
                ]
            };
        }

        return new PerformSaveResult
        {
            IsSuccess = true,
            Errors = []
        };
    }

    public class PerformSaveResult
    {
        public bool IsSuccess { get; init; } = false;
        public BasicErrorItem[] Errors { get; init; } = [];
    }

    private async Task PerformSaveValidateLogChannel(
        ulong guildId,
        PerformSaveOptions options,
        List<BasicErrorItem> errors)
    {
        if (options.LogOpt == SaveRolePreserveLogCfgKind.Disabled ||
            options.LogOpt == SaveRolePreserveLogCfgKind.Fallback)
            return;

        var logChannelValue = options.LogChannel.GetValueOrDefault(0);
        if (options.LogOpt == SaveRolePreserveLogCfgKind.Custom)
        {
            if (logChannelValue == 0)
            {
                errors.Add(new BasicErrorItem("Log Channel", "Value is required"));
                return;
            }


            var guild = ExceptionHelper.RetryOnTimedOut(() => _client.GetGuild(guildId));
            var channel = guild.Channels.FirstOrDefault(e => e.Id == logChannelValue);
            if (channel == null)
            {
                errors.Add(new BasicErrorItem("Log Channel", "Does not exist"));
                return;
            }

            var channelPermissions = DiscordPermissionHelper.CalculateChannel(guild.CurrentUser, channel);
            var missingPermissions = new HashSet<string>();
            if (!channelPermissions.CanPerform(ChannelPermission.SendMessages))
            {
                missingPermissions.Add("Send Messages");
            }

            if (missingPermissions.Count > 0)
            {
                errors.Add(new BasicErrorItem("Log Channel", "Xenia is missing the following permissions: " + string.Join(", ", missingPermissions)));
            }
        }
    }

    public sealed record BasicErrorItem(
        string Name,
        string Text,
        bool IsFatal = true);

    public sealed record PerformSaveOptions(
        bool Enable,
        ulong[] RoleBlacklist,
        SaveRolePreserveLogCfgKind LogOpt,
        ulong? LogChannel);

    public enum SaveRolePreserveLogCfgKind
    {
        Disabled,
        Fallback,
        Custom
    }

    // TODO implement logic to return _PartialBlacklistedRoles for HTMX
    // TODO implement logic to return _PartialEnableFlag for HTMX
    // TODO implement logic to save blacklisted roles, and return _PartialBlacklistedRoles
    // TODO implement logic to save "Enable" and return _PartialEnableFlag

    // TODO for role preserve logging - implement logic to return _PartialLogChannel for HTMX
    // TODO for role preserve logging - add logic to fetch StrippedChannel? for current log channel
    // TODO for role preserve logging - add logic to fetch available channels for selecting new log channel
    // TODO for role preserve logging - implement logic to save log channel

    private async Task<StrippedGuild> GetStrippedGuild(ulong guildId)
    {
        var discordGuild = ExceptionHelper.RetryOnTimedOut(() => _client.GetGuild(guildId));
        if (discordGuild != null) return StrippedGuild.FromGuild(discordGuild);
        
        var guildIdStr = guildId.ToString();
        var snapshot = await _db.GuildSnapshots
            .AsNoTracking()
            .Where(e => e.GuildId == guildIdStr)
            .OrderByDescending(e => e.RecordCreatedAt)
            .FirstOrDefaultAsync();
        return StrippedGuild.FromExisting(null, snapshot, guildId);
    }

    private async Task<List<StrippedRole>> GetAvailableRoles(ulong guildId)
    {
        var guildIdStr = guildId.ToString();
        var discordGuild = ExceptionHelper.RetryOnTimedOut(() => _client.GetGuild(guildId));
        if (discordGuild != null)
        {
            return discordGuild.Roles
                .Select(StrippedRole.FromRole)
                .OrderBy(e => e.Position)
                .ToList();
        }

        var records = await _db.GuildRoleCache
            .Include(e => e.Snapshot)
            .ThenInclude(e => e.Permissions)
            .AsNoTracking()
            .Where(e => e.GuildId == guildIdStr && !e.IsDeleted)
            .OrderBy(e => e.Position)
            .Select(e => e.Snapshot)
            .ToArrayAsync();
        return records.Select(StrippedRole.FromRole).ToList();
    }

    private async Task<List<StrippedRole>> GetRoleBlacklist(ulong guildId)
    {
        var guildIdStr = guildId.ToString();
        var roleIds = await _db.RolePreserveBlacklistedRoles.AsNoTracking()
            .Where(e => e.GuildId == guildIdStr)
            .OrderBy(e => e.RoleId)
            .Select(e => e.RoleId)
            .ToArrayAsync();
        var roleSnapshots = await _db.GuildRoleCache
            .AsNoTracking()
            .Include(e => e.Snapshot)
            .ThenInclude(e => e.Permissions)
            .Where(e => ((IEnumerable<string>)roleIds).Contains(e.RoleId))
            .Select(e => e.Snapshot)
            .ToListAsync();
        var result = roleSnapshots.Select(StrippedRole.FromRole).ToList();
        
        foreach (var roleId in roleIds.Where(e => roleSnapshots.All(x => x.RoleId != e)))
        {
            var id = roleId.ParseULong(false);
            if (!id.HasValue) continue;
            result.Add(new StrippedRole()
            {
                Id = id.Value,
                Name = $"{id}"
            });
        }

        return result.OrderBy(e => e.Id).ToList();
    }

    private async Task<RolePreserveGuildModel?> GetRolePreserveGuild(ulong guildId)
    {
        var guildIdStr = guildId.ToString();
        return await _db.RolePreserveGuilds
            .AsNoTracking()
            .Include(e => e.BlacklistedRoles)
            .FirstOrDefaultAsync(e => e.GuildId == guildIdStr);
    }
}
