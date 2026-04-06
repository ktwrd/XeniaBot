using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Core.Helpers;
using XeniaDiscord.Data;
using XeniaDiscord.Data.Repositories;

namespace XeniaBot.Core.Modules;

[Group("rolepreserve", "Configure the RolePreserve module.")]
[CommandContextType(InteractionContextType.Guild)]
public class RolePreserveModule : InteractionModuleBase
{
    private readonly XeniaDbContext _db;
    private readonly RolePreserveGuildRepository _userRepository;
    public RolePreserveModule(IServiceProvider services)
    {
        _db = services.GetRequiredScopedService<XeniaDbContext>(out var scope);
        _userRepository = (scope?.ServiceProvider ?? services).GetRequiredService<RolePreserveGuildRepository>();
    }
    [SlashCommand("enable", "Grant members preserved roles on re-join.")]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    public async Task Enable()
    {
        await DeferAsync();
        await using var db = _db.CreateSession();
        await using var trans = await db.Database.BeginTransactionAsync();
        try
        {
            await _userRepository.EnableAsync(db, Context.Guild.Id, true);
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

    [SlashCommand("disable", "Disable Role Grant feature.")]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    public async Task Disable()
    {
        await DeferAsync();
        await using var db = _db.CreateSession();
        await using var trans = await db.Database.BeginTransactionAsync();
        try
        {
            await _userRepository.EnableAsync(db, Context.Guild.Id, false);
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
}