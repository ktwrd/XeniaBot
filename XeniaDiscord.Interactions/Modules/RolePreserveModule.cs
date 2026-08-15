using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using NLog;
using XeniaBot.Shared;
using XeniaBot.Shared.Helpers;
using XeniaBot.Shared.Services;
using XeniaDiscord.Data;
using XeniaDiscord.Data.Repositories;
using XeniaDiscord.Interactions.Helpers;

namespace XeniaDiscord.Interactions.Modules;

[Group("rolepreserve", "Configure the RolePreserve module.")]
[RequireBotPermission(GuildPermission.ManageRoles)]
[CommandContextType(InteractionContextType.Guild)]
[UsedImplicitly]
public class RolePreserveModule : InteractionModuleBase
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly RolePreserveGuildRepository _repo;
    private readonly ErrorReportService _error;
    private readonly ConfigData _config;
    private readonly IDbContextFactory<XeniaDbContext> _dbFactory;
    public RolePreserveModule(IServiceProvider services)
    {
        try
        {
            _repo = services.GetRequiredService<RolePreserveGuildRepository>();
            _error = services.GetRequiredService<ErrorReportService>();
            _config = services.GetRequiredService<ConfigData>();
            _dbFactory = services.GetRequiredService<IDbContextFactory<XeniaDbContext>>();
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
        await using var db = await _dbFactory.CreateDbContextAsync();
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
        await using var db = await _dbFactory.CreateDbContextAsync();
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
        await using var db = await _dbFactory.CreateDbContextAsync();
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
        await using var db = await _dbFactory.CreateDbContextAsync();
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
            await using var db = await _dbFactory.CreateDbContextAsync();
            var (embed, components) = await RolePreserveModuleHelper.ListEmbed(_config, db, Context.Guild, page);
            if (components != null)
            {
                await FollowupAsync(
                    embed: embed.Build(),
                    components: components.Build());
            }
            else
            {
                await FollowupAsync(embed: embed.Build());
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex);
        }
    }
}