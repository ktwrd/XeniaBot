using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using NLog;
using XeniaBot.Shared;
using XeniaBot.Shared.Helpers;
using XeniaBot.Shared.Services;
using XeniaDiscord.Data;
using XeniaDiscord.Data.Repositories;

namespace XeniaBot.Core.Modules;

[Group("rolepreserve", "Configure the RolePreserve module.")]
[RequireBotPermission(GuildPermission.ManageRoles | GuildPermission.ModerateMembers)]
[CommandContextType(InteractionContextType.Guild)]
[UsedImplicitly]
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
    [UsedImplicitly]
    public async Task Enable()
    {
        await DeferAsync();
        await using var db = _db.CreateSession();
        await using var trans = await db.Database.BeginTransactionAsync();
        try
        {
            var guildUser =
                await ExceptionHelper.RetryOnTimedOut(async () => await Context.Guild.GetUserAsync(Context.User.Id))
                ?? throw new InvalidOperationException($"Could not find requestors user in the current guild (guildId={Context.Guild.Id}, userId={Context.User.Id})");

            await _repo.EnableAsync(db, Context.Guild.Id, true, guildUser);

            await db.SaveChangesAsync();
            await trans.CommitAsync();
            
            await FollowupAsync(embed: new EmbedBuilder()
                .WithTitle("Role Preserve - Enable")
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
                .AddField("Error", ex.Message[..Math.Min(1024, ex.Message.Length)])
                .WithColor(Color.Red)
                .Build());
            await _error.Submit(new ErrorReportBuilder()
                .WithNotes("Failed to enable role preservation")
                .WithContext(Context)
                .WithException(ex));
        }
    }

    [SlashCommand("disable", "Disable Role Preservation feature.")]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    [RegisterDBLCommand]
    [UsedImplicitly]
    public async Task Disable()
    {
        await DeferAsync();
        await using var db = _db.CreateSession();
        await using var trans = await db.Database.BeginTransactionAsync();
        try
        {
            var guildUser =
                await ExceptionHelper.RetryOnTimedOut(async () => await Context.Guild.GetUserAsync(Context.User.Id))
                ?? throw new InvalidOperationException($"Could not find requestors user in the current guild (guildId={Context.Guild.Id}, userId={Context.User.Id})");
            
            await _repo.EnableAsync(db, Context.Guild.Id, false, guildUser);
            
            await db.SaveChangesAsync();
            await trans.CommitAsync();
            
            await FollowupAsync(embed: new EmbedBuilder()
                .WithTitle("Role Preserve - Disable")
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
                .AddField("Error", ex.Message[..Math.Min(1024, ex.Message.Length)])
                .WithColor(Color.Red)
                .Build());
            await _error.Submit(new ErrorReportBuilder()
                .WithNotes("Failed to disable role preservation")
                .WithContext(Context)
                .WithException(ex));
        }
    }
    
    [SlashCommand("blacklist-add", "Add a role to be ignored with role preservation")]
    [RequireUserPermission(GuildPermission.ManageRoles)]
    [RequireBotPermission(GuildPermission.ManageRoles | GuildPermission.ModerateMembers)]
    [RegisterDBLCommand]
    [UsedImplicitly]
    public async Task BlacklistAdd(
        IRole role)
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
    [SlashCommand("blacklist-add", "Remove a ignored/blacklisted role")]
    [RequireUserPermission(GuildPermission.ManageRoles)]
    [RequireBotPermission(GuildPermission.ManageRoles | GuildPermission.ModerateMembers)]
    [RegisterDBLCommand]
    [UsedImplicitly]
    public async Task BlacklistRemove(IRole role)
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

            var guildUser =
                await ExceptionHelper.RetryOnTimedOut(async () => await Context.Guild.GetUserAsync(Context.User.Id))
                ?? throw new InvalidOperationException($"Could not find requestors user in the current guild (guildId={Context.Guild.Id}, userId={Context.User.Id})");

            var result = await _repo.RoleBlacklistRemove(db, role.Guild.Id, role.Id, guildUser);
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
                    embed.WithDescription($"Role is not blacklisted: <@&{role.Id}>")
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

    [SlashCommand("blacklist", "List ignored/blacklisted roles")]
    [RequireUserPermission(GuildPermission.ManageRoles)]
    [RegisterDBLCommand]
    [UsedImplicitly]
    public async Task BlacklistList(int page = 1)
    {
        await DeferAsync();
        var (embed, components) = await ListEmbed(Context.Guild, page);
        await FollowupAsync(
            embed: embed.Build(),
            components: components.Build());
        // throw new NotImplementedException(
        //     "List roles (basic), and implement pagination. Should only show 15 roles per page, and it should have a CSV export function. " +
        //     "Data should be fetched with XeniaDiscord.Data.Repositories.RolePreserveGuildRepository.GetBlacklistRolesForGuild(XeniaDbContext, ulong)");
    }

    private sealed record ListEmbedResult(EmbedBuilder Embed, ComponentBuilderV2 ComponentBuilder);
    private async Task<ListEmbedResult> ListEmbed(
        IGuild guild,
        int page = 1)
    {
        const int pageSize = 10;
        const string emptyPageMessageFmt =  "No roles on page `{0}`\nRun `/rolepreserve blacklist` again to see the blacklisted roles."; 
        
        page = int.Max(1, page);
        var skip = pageSize * (page - 1);
        var guildIdStr = guild.Id.ToString();

        await using var db = _db.CreateSession();
        var totalItemCount = await db.RolePreserveBlacklistedRoles
            .Where(e => e.GuildId == guildIdStr)
            .CountAsync();
        var lastPage = Math.Max(1, Convert.ToInt32(Math.Ceiling(totalItemCount / (float)pageSize)));
        
        var components = BuildBlacklistedRolesListingComponents(page, lastPage);
        
        var embed = new EmbedBuilder()
            .WithTitle("Role Preserve - Blacklisted Roles")
            .WithColor(Color.Blue)
            .WithFooter($"Page {page}")
            .WithCurrentTimestamp();
        
        // only return early if we're past the last page
        if (page > lastPage)
        {
            embed.Description = string.Format(emptyPageMessageFmt, page);
            return new(embed, components);
        }

        var items = await db.RolePreserveBlacklistedRoles
            .Where(e => e.GuildId == guildIdStr)
            .OrderByDescending(e => e.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();
        
        if (items.Count < 1)
        {
            embed.Description = page == 1
                ? "No blacklisted roles have been setup. You can do that with `/rolepreserve blacklist-add`"
                : string.Format(emptyPageMessageFmt, page);
        }
        else
        {
            embed.Description = string.Join("\n", items.Select(e => $"- <@&{e.RoleId}>"));
        }

        return new(embed, components);
    }

    private static ComponentBuilderV2 BuildBlacklistedRolesListingComponents(
        int currentPage,
        int lastPage)
    {
        var paginationRow = new List<ButtonBuilder>();
        if (currentPage > 2)
        {
            paginationRow.Add(new ButtonBuilder()
                .WithCustomId(ViewBlacklistedRolesInteractionName.Replace("*", "1"))
                .WithLabel("Start")
                .WithStyle(ButtonStyle.Primary));
        }
        if (currentPage > 1)
        {
            paginationRow.Add(new ButtonBuilder()
                .WithCustomId(ViewBlacklistedRolesInteractionName.Replace("*", (currentPage - 1).ToString()))
                .WithLabel("Previous")
                .WithStyle(ButtonStyle.Primary));
        }
        if (currentPage < lastPage)
        {
            paginationRow.Add(new ButtonBuilder()
                .WithCustomId(ViewBlacklistedRolesInteractionName.Replace("*", (currentPage + 1).ToString()))
                .WithLabel("Next")
                .WithStyle(ButtonStyle.Primary));
        }
        if (currentPage < lastPage - 1)
        {
            paginationRow.Add(new ButtonBuilder()
                .WithCustomId(ViewBlacklistedRolesInteractionName.Replace("*", lastPage.ToString()))
                .WithLabel("Last")
                .WithStyle(ButtonStyle.Primary));
        }
        return new ComponentBuilderV2()
            .WithActionRow(paginationRow);
    }

    [ComponentInteraction("rolepreserve_blacklistedroles_list:page=*")]
    [RequireUserPermission(GuildPermission.ManageRoles)]
    [UsedImplicitly]
    public async Task ViewBlacklistedRolesInteraction(int page)
    {
        await DeferAsync();
        var (embed, components) = await ListEmbed(Context.Guild, page);
        await FollowupAsync(
            embed: embed.Build(),
            components: components.Build());
    }

    private const string ViewBlacklistedRolesInteractionName = "rolepreserve_blacklistedroles_list:page=*";
}