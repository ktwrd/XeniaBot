using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
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
[RequireBotPermission(GuildPermission.ManageRoles)]
[CommandContextType(InteractionContextType.Guild)]
[UsedImplicitly]
public class RolePreserveModule : InteractionModuleBase
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly XeniaDbContext _db;
    private readonly RolePreserveGuildRepository _repo;
    private readonly ErrorReportService _error;
    private readonly ConfigData _config;
    public RolePreserveModule(IServiceProvider services)
    {
        try
        {
            _db = services.GetRequiredService<XeniaDbContext>();
            _repo = services.GetRequiredService<RolePreserveGuildRepository>();
            _error = services.GetRequiredService<ErrorReportService>();
            _config = services.GetRequiredService<ConfigData>();
        }
        catch (Exception ex)
        {
            _log.Error(ex);
            throw new InvalidOperationException("Failed to get services", ex);
        }
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
    
    [SlashCommand("blacklist-add", "Ignore/blacklist a role")]
    [RequireAnyUserPermission(GuildPermission.ManageGuild | GuildPermission.ManageRoles)]
    [RegisterDBLCommand]
    [UsedImplicitly]
    public async Task BlacklistAdd(IRole role)
    {
        await DeferAsync();
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
                    embed.WithDescription($"Added role {role.Mention} to the blacklist.")
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
            var msg = $"Failed to add role {role.Name} to blacklist (guildId={Context.Guild.Id}, roleId={role.Id})";
            _log.Error(ex, msg);
            await trans.RollbackAsync();
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
    
    [SlashCommand("blacklist-remove", "Remove a ignored/blacklisted role")]
    [RequireAnyUserPermission(GuildPermission.ManageGuild | GuildPermission.ManageRoles)]
    [RegisterDBLCommand]
    [UsedImplicitly]
    public async Task BlacklistRemove(IRole role)
    {
        await DeferAsync();
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
            var msg = $"Failed to remove role {role.Name} from blacklist (roleId={role.Id})";
            _log.Error(ex, msg);
            await trans.RollbackAsync();
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
    [RequireAnyUserPermission(GuildPermission.ManageGuild | GuildPermission.ManageRoles)]
    [RegisterDBLCommand]
    [UsedImplicitly]
    public async Task BlacklistList(int page = 1)
    {
        try
        {
            await DeferAsync();
            await using var db = _db.CreateSession();
            var (embed, components) = await RolePreserveModuleHelper.ListEmbed(_config, db, Context.Guild, page);
            if (components != null)
            {
                await FollowupAsync(
                    embed: embed.Build(),
                    components: components.Build());
            }
            else
            {
                await FollowupAsync(
                    embed: embed.Build());
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex);
        }
    }
}

internal static class RolePreserveModuleHelper
{
    internal const string ViewBlacklistedRolesInteractionName = "rolepreserve_blacklistedroles_list:page=*";
    internal sealed record ListEmbedResult(EmbedBuilder Embed, ComponentBuilderV2? ComponentBuilder);
    internal static async Task<ListEmbedResult> ListEmbed(
        ConfigData config,
        XeniaDbContext db,
        IGuild guild,
        int page = 1)
    {
        const int pageSize = 10;

        page = int.Max(1, page);
        var skip = pageSize * (page - 1);
        var guildIdStr = guild.Id.ToString();

        var totalItemCount = await db.RolePreserveBlacklistedRoles
            .Where(e => e.GuildId == guildIdStr)
            .CountAsync();
        var lastPage = Math.Max(1, Convert.ToInt32(Math.Ceiling(totalItemCount / (float)pageSize)));
        
        var components = BuildBlacklistedRolesListingComponents(page, lastPage);

        var pageFmt = page.ToString("N0");
        var embed = new EmbedBuilder()
            .WithTitle("Role Preserve - Blacklisted Roles")
            .WithColor(Color.Blue)
            .WithFooter("Page " + pageFmt)
            .WithCurrentTimestamp();
        
        // only return early if we're past the last page
        if (page > lastPage)
        {
            embed.Description = DescriptionForEmptyPage(config, guild.Id, pageFmt);
            return new ListEmbedResult(embed, components);
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
                : DescriptionForEmptyPage(config, guild.Id, pageFmt);
        }
        else
        {
            embed.Description = string.Join("\n", items.Select(e => $"<@&{e.RoleId}>"));
        }

        return new ListEmbedResult(embed, components);
    }

    private static string DescriptionForEmptyPage(ConfigData config, ulong guildId, string pageFmt)
    {
        const string emptyPageMessageFmt = "No roles on page `{0}`\nRun `/rolepreserve blacklist` again to see the blacklisted roles.";
        const string emptyPageMessageWithDashFmt = emptyPageMessageFmt + "\n\nYou can also see what roles are blacklisted [via the web panel]({1}/Guild/{2}/RolePreserve/Settings).";
        if (config.HasDashboard)
            return string.Format(emptyPageMessageWithDashFmt,
                pageFmt,
                config.DashboardUrl,
                guildId);
        return string.Format(emptyPageMessageFmt, pageFmt);
    }
    private static ComponentBuilderV2? BuildBlacklistedRolesListingComponents(
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
        if (paginationRow.Count < 1) return null;
        return new ComponentBuilderV2()
            .WithActionRow(paginationRow);
    }
}

public class RolePreserveComponentModule : InteractionModuleBase<SocketInteractionContext<SocketMessageComponent>>
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly XeniaDbContext _db;
    private readonly ConfigData _config;
    public RolePreserveComponentModule(IServiceProvider services)
    {
        try
        {
            _db = services.GetRequiredService<XeniaDbContext>();
        }
        catch (Exception ex)
        {
            _log.Error(ex);
            throw new InvalidOperationException("Failed to get services", ex);
        }

        _config = services.GetRequiredService<ConfigData>();
    }
    
    [ComponentInteraction(RolePreserveModuleHelper.ViewBlacklistedRolesInteractionName)]
    [RequireUserPermission(GuildPermission.ManageRoles)]
    [UsedImplicitly]
    public async Task ListComponent(int page = 1)
    {
        try
        {
            await using var db = _db.CreateSession();
            var (embed, components) = await RolePreserveModuleHelper.ListEmbed(_config, db, Context.Guild, page);
            await Context.Interaction.UpdateAsync(
                p =>
                {
                    p.Embed = embed.Build();
                    p.Components = components == null ? new Optional<MessageComponent>(): components.Build();
                });
        }
        catch (Exception ex)
        {
            _log.Error(ex);
        }
    }
}