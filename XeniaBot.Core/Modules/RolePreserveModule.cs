using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using NLog;
using XeniaBot.Core.Helpers;
using XeniaBot.Shared;
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
        
        await using var db = _db.CreateSession();
        await using var trans = await db.Database.BeginTransactionAsync();
        try
        {

            
            if (action != BlacklistAction.List)
            {
                await db.SaveChangesAsync();
                await trans.CommitAsync();
            }

            throw new NotImplementedException();
        }
        catch (Exception ex)
        {
            if (action != BlacklistAction.List)
            {
                await trans.RollbackAsync();
            }
            var msg = $"Failed to perform action \"{action}\" on role {role?.Name} ({role?.Id})";
            _log.Error(ex, msg);
            await _error.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes(msg)
                .WithContext(Context));
            await FollowupAsync("Failed to perform action!");
        }

        throw new NotImplementedException();
    }

    public enum BlacklistAction
    {
        List,
        Add,
        Remove
    }
}