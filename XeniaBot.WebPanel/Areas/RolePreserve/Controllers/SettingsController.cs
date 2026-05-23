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
using XeniaBot.WebPanel.Areas.RolePreserve.Models.Settings;
using XeniaBot.WebPanel.Models;
using XeniaDiscord.Common.Helpers;
using XeniaDiscord.Data;
using XeniaDiscord.Data.Models.RolePreserve;

namespace XeniaBot.WebPanel.Areas.RolePreserve.Controllers;

[Authorize]
[AuthRequired]
[Area("RolePreserve")]
[Route("~/Guild/{guildId}/[area]/[controller]")]
[RestrictToGuild(GuildIdRouteKey = "guildId")]
public class SettingsController : Controller
{
    private readonly DiscordSocketClient _client;
    private readonly XeniaDbContext _db;

    public SettingsController(IServiceProvider services)
    {
        _client = services.GetRequiredService<DiscordSocketClient>();
        _db = services.GetRequiredService<XeniaDbContext>();
    }

    [Route("", Name = "Guild_RolePreserve_Settings_Index")]
    public async Task<IActionResult> Index(ulong guildId)
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

        return View("Default", vm);
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
        throw new NotImplementedException();
    }

    private async Task<PerformSaveResult> PerformSave(
        ulong guildId, 
        PerformSaveOptions options)
    {
        var errors = new List<BasicErrorItem>();
        await PerformSaveValidateLogChannel(guildId, options, errors);

        if (errors.Count > 0)
        {
            return new PerformSaveResult
            {
                Errors = errors.ToArray()
            };
        }
        
        // remove items in options.RoleBlacklist for ids that don't exist in discord or cache or snapshots.
        // remove items in options.RoleBlacklist where the associated guild isn't guildId

        // 1. make new db session
        // 2. start transaction
        // 3. save guild to cache/snapshot
        // 4. refresh cache/snapshot for all referenced roles & tables
        // 5. refresh cache (if needed) for requestor discord user
        // 6. create or update RolePreserveGuildModel w/ new enable state
        // 7. find what blacklisted roles need to be added/removed
        // 8. (in something like RolePreserveService) send log message saying that enable/disable state was changed, and/or blacklisted roles were added/removed
        throw new NotImplementedException();
    }

    public class PerformSaveResult
    {
        public BasicErrorItem[] Errors { get; set; } = [];
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
