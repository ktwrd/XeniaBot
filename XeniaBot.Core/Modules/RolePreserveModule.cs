using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NLog;
using XeniaBot.Core.Helpers;
using XeniaBot.Shared;
using XeniaBot.Shared.Helpers;
using XeniaBot.Shared.Services;
using XeniaDiscord.Data;
using XeniaDiscord.Data.Repositories;

namespace XeniaBot.Core.Modules;

[Group("rolepreserve", "Configure the RolePreserve module.")]
[RequireBotPermission(GuildPermission.ManageRoles | GuildPermission.ModerateMembers)]
[CommandContextType(InteractionContextType.Guild)]
public class RolePreserveModule : InteractionModuleBase
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly XeniaDbContext _db;
    private readonly RolePreserveGuildRepository _repo;
    private readonly ErrorReportService _error;
    public RolePreserveModule(IServiceProvider services)
    {
        _db = services.GetRequiredScopedService<XeniaDbContext>(out var scope);
        _repo = (scope?.ServiceProvider ?? services).GetRequiredService<RolePreserveGuildRepository>();
        _error = services.GetRequiredService<ErrorReportService>();
    }
    [SlashCommand("enable", "Grant members preserved roles on re-join.")]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    [RegisterDBLCommand]
    public async Task Enable()
    {
        await DeferAsync();
        await using var db = _db.CreateSession();
        await using var trans = await db.Database.BeginTransactionAsync();
        try
        {
            await _repo.EnableAsync(db, Context.Guild.Id, true);
            await db.SaveChangesAsync();
            await trans.CommitAsync();
            
            await FollowupAsync(embed: new EmbedBuilder()
                .WithTitle("Role Preserve")
                .WithDescription("Roles will now be granted to members when they re-join (if Xenia can grant those roles)")
                .WithColor(Color.Blue)
                .Build());
        }
        catch (Exception ex)
        {
            await trans.RollbackAsync();
            await FollowupAsync(embed: new EmbedBuilder()
                .WithTitle("Role Preserve - Failed to Enable")
                .WithDescription("Failed to enable Role Preserve feature")
                .AddField("Message", ex.Message[..Math.Min(1024, ex.Message.Length)])
                .WithColor(Color.Red)
                .Build());
            await DiscordHelper.ReportError(ex, Context);
        }
    }

    [SlashCommand("disable", "Disable Role Preservation feature.")]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    [RegisterDBLCommand]
    public async Task Disable()
    {
        await DeferAsync();
        await using var db = _db.CreateSession();
        await using var trans = await db.Database.BeginTransactionAsync();
        try
        {
            await _repo.EnableAsync(db, Context.Guild.Id, false);
            await db.SaveChangesAsync();
            await trans.CommitAsync();
            
            await FollowupAsync(embed: new EmbedBuilder()
                .WithTitle("Role Preserve")
                .WithDescription("Xenia will continue to track member roles, but they will not be granted when someone re-joins.")
                .WithColor(Color.Blue)
                .Build());
        }
        catch (Exception ex)
        {
            await trans.RollbackAsync();
            await FollowupAsync(embed: new EmbedBuilder()
                .WithTitle("Role Preserve - Failed to Disable")
                .WithDescription("Failed to disable Role Preserve feature")
                .AddField("Message", ex.Message[..Math.Min(1024, ex.Message.Length)])
                .WithColor(Color.Red)
                .Build());
            await DiscordHelper.ReportError(ex, Context);
        }
    }

    [SlashCommand("ignore", "Add/Remove/List Ignored Roles")]
    [RequireUserPermission(GuildPermission.ManageRoles)]
    [RegisterDBLCommand]
    public async Task Blacklist(
        BlacklistAction action,
        IRole? role = null)
    {
        if (action == BlacklistAction.Add && role == null)
        {
            await RespondAsync("Target role (`role`) is required when adding a role to the blacklist.");
            return;
        }
        if (action == BlacklistAction.Remove && role == null)
        {
            await RespondAsync("Target role (`role`) is required when removing a role from the blacklist.");
            return;
        }


        await DeferAsync();
        switch (action)
        {
            case BlacklistAction.Add:
                await BlacklistAdd(role);
                break;
            case BlacklistAction.Remove:
                await BlacklistRemove(role);
                break;
            case BlacklistAction.List:
                await BlacklistList(role);
                break;
        }
    }

    private async Task BlacklistAdd(
        [NotNull] IRole role)
    {
        const string title = "Role Preserve - Add to blacklist";
        await using var db = _db.CreateSession();
        await using var trans = await db.Database.BeginTransactionAsync();
        try
        {
            if (role.Guild.Id != Context.Guild.Id)
            {
                await FollowupAsync(embed: new EmbedBuilder()
                    .WithTitle(title)
                    .WithDescription("Hey! Why are you trying to add a role to a different guild's blacklist?")
                    .WithColor(Color.Orange)
                    .WithCurrentTimestamp()
                    .Build());
            }

            var guildUser =
                await ExceptionHelper.RetryOnTimedOut(async () => await Context.Guild.GetUserAsync(Context.User.Id))
                ?? throw new InvalidOperationException($"Could not find requestors user in the current guild (guildId={Context.Guild.Id}, userId={Context.User.Id})");
            var result = await _repo.RoleBlacklistAdd(db, role.Guild, role, guildUser);
            await db.SaveChangesAsync();
            await trans.CommitAsync();

            var embed = new EmbedBuilder()
                .WithTitle(title)
                .WithFooter($"Role Id: {role.Id}")
                .WithCurrentTimestamp();
            switch (result)
            {
                case RolePreserveGuildRepository.RoleBlacklistAddResult.Ok:
                    embed.WithDescription($"Removed role {role.Mention} from the blacklist.")
                         .WithColor(Color.Green);
                    break;
                case RolePreserveGuildRepository.RoleBlacklistAddResult.GuildMismatch:
                    embed.WithDescription("Hey! Why are you trying to add a role to a different guild's blacklist?")
                         .WithColor(Color.Orange);
                    break;
                case RolePreserveGuildRepository.RoleBlacklistAddResult.AlreadyExists:
                    embed.WithDescription($"Role is already blacklisted: {role.Id}")
                         .WithColor(Color.Orange);
                    break;
                default:
                    embed.WithDescription($"{result}\n-# Could not format result code")
                        .WithColor(Color.Purple);
                    break;
            }
            await FollowupAsync(embed: embed.Build());
        }
        catch (Exception ex)
        {
            await trans.RollbackAsync();
            var msg = $"Failed to add role {role.Name} to blacklist (guildId={Context.Guild.Id}, roleId={role.Id})";
            _log.Error(ex, msg);
            await _error.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes(msg)
                .WithContext(Context));
            await FollowupAsync(embed: new EmbedBuilder()
                .WithTitle(title)
                .WithDescription($"Failed to add role {role.Mention} to blacklist.\n-# {ex.GetType().Name}")
                .WithColor(Color.Red)
                .WithCurrentTimestamp()
                .Build());
        }
    }

    private async Task BlacklistRemove(
        [NotNull] IRole role)
    {
        const string title = "Role Preserve - Remove from blacklist";
        await using var db = _db.CreateSession();
        await using var trans = await db.Database.BeginTransactionAsync();
        try
        {
            if (role.Guild.Id != Context.Guild.Id)
            {
                await FollowupAsync(embed: new EmbedBuilder()
                    .WithTitle(title)
                    .WithDescription("Hey! Why are you trying to remove a role from a different guild's blacklist?")
                    .WithColor(Color.Orange)
                    .WithCurrentTimestamp()
                    .Build());
            }

            var result = await _repo.RoleBlacklistRemove(db, role.Guild.Id, role.Id);
            await db.SaveChangesAsync();
            await trans.CommitAsync();

            var embed = new EmbedBuilder()
                .WithTitle(title)
                .WithFooter($"Role Id: {role.Id}")
                .WithCurrentTimestamp();
            switch (result)
            {
                case RolePreserveGuildRepository.RoleBlacklistRemoveResult.Ok:
                    embed.WithDescription($"Removed role {role.Mention} from the blacklist.")
                         .WithColor(Color.Green);
                    break;
                case RolePreserveGuildRepository.RoleBlacklistRemoveResult.NotFound:
                    embed.WithDescription($"Role is not blacklisted: {role.Id}")
                         .WithColor(Color.Orange);
                    break;
                default:
                    embed.WithDescription($"{result}\n-# Could not format result code")
                        .WithColor(Color.Purple);
                    break;
            }
            await FollowupAsync(embed: embed.Build());
        }
        catch (Exception ex)
        {
            await trans.RollbackAsync();
            var msg = $"Failed to remove role {role.Name} from blacklist (roleId={role.Id})";
            _log.Error(ex, msg);
            await _error.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes(msg)
                .WithContext(Context));
            await FollowupAsync(embed: new EmbedBuilder()
                .WithTitle(title)
                .WithDescription($"Failed to remove role {role.Mention} from blacklist.\n-# {ex.GetType().Name}")
                .WithColor(Color.Red)
                .WithCurrentTimestamp()
                .Build());
        }
    }

    private async Task BlacklistList(
        IRole? role)
    {
        var embed = await ListEmbed(Context.Guild);
        throw new NotImplementedException(
            "List roles (basic), and implement pagination. Should only show 15 roles per page, and it should have a CSV export function. " +
            "Data should be fetched with XeniaDiscord.Data.Repositories.RolePreserveGuildRepository.GetBlacklistRolesForGuild(XeniaDbContext, ulong)");
    }

    private async Task<EmbedBuilder> ListEmbed(
        IGuild guild,
        int page = 1)
    {
        page = int.Max(1, page);
        const int pageSize = 10;
        var guildIdStr = guild.Id.ToString();
        await using var db = _db.CreateSession();
        var skip = pageSize * (page - 1);
        var items = await db.RolePreserveBlacklistedRoles
            .Where(e => e.GuildId == guildIdStr)
            .OrderByDescending(e => e.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();
        var embed = new EmbedBuilder()
            .WithTitle("Role Preserve - Blacklisted Roles")
            .WithDescription(string.Join("\n", items.Select(e => $"- <@&{e.RoleId}>")))
            .WithColor(Color.Blue)
            .WithFooter($"Page {page}")
            .WithCurrentTimestamp();
        // generate embed, and include pagination buttons.
        return embed;
    }

    public enum BlacklistAction
    {
        List,
        Add,
        Remove
    }
}