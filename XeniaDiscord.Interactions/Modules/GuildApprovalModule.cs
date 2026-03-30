using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using XeniaBot.Shared.Services;
using XeniaDiscord.Common.Services;
using XeniaDiscord.Data;

namespace XeniaDiscord.Interactions.Modules;

[CommandContextType(InteractionContextType.Guild)]
public class GuildApprovalModule : InteractionModuleBase
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly XeniaDbContext _db;
    private readonly ErrorReportService _err;
    private readonly GuildApprovalService _service;
    public GuildApprovalModule(IServiceProvider services)
    {
        _db = services.GetRequiredScopedService<XeniaDbContext>(out var scope);
        _err = services.GetRequiredService<ErrorReportService>();
        _service = services.GetRequiredService<GuildApprovalService>();
    }

    [SlashCommand("approve", "Approve a user")]
    [RequireUserPermission(GuildPermission.ManageRoles)]
    [RequireBotPermission(GuildPermission.ManageRoles)]
    public async Task ApproveUser(IGuildUser user)
    {
        await DeferAsync();
        var embed = new EmbedBuilder()
            .WithTitle("Approve User")
            .WithColor(Color.Blue);
        try
        {
            var guildIdStr = Context.Guild.Id.ToString();
            var config = await _db.GuildApprovals.AsNoTracking().FirstOrDefaultAsync(e => e.GuildId == guildIdStr);

            if (config == null)
            {
                embed.WithDescription($"Approval system has not been configured.").WithColor(Color.Red);
                await FollowupAsync(embed: embed.Build());
                return;
            }
            if (!config.Enabled)
            {
                embed.WithDescription("Approval system is not enabled.").WithColor(Color.Red);
                await FollowupAsync(embed: embed.Build());
                return;
            }

            var roleId = config.GetApprovedRoleId();
            if (!roleId.HasValue)
            {
                embed.WithDescription("\"Approved Role\" has not been configured.").WithColor(Color.Red);
                await FollowupAsync(embed: embed.Build());
                return;
            }
            var role = await Context.Guild.GetRoleAsync(roleId.Value);
            if (role == null)
            {
                embed.WithDescription($"Could not find role: `{roleId.Value}` <@&{roleId.Value}>").WithColor(Color.Red);
                await FollowupAsync(embed: embed.Build());
                return;
            }

            var targetFormatted = user.Username + (string.IsNullOrEmpty(user.Discriminator.Trim('0')?.Trim()) ? "" : $"#{user.Discriminator}");
            var invokerFormatted = Context.User.Username + (string.IsNullOrEmpty(Context.User.Discriminator.Trim('0')?.Trim()) ? "" : $"#{Context.User.Discriminator}");
            if (user.RoleIds.Contains(role.Id))
            {
                embed.WithDescription($"User already has been approved!\n-# user: `{targetFormatted}` ({user.Id})\n-# role: {role.Mention}")
                .WithColor(Color.Orange);
            }
            else
            {
                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        await user.AddRoleAsync(role);
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (!ex.ToString().ToLower().Contains("timed out"))
                        {
                            break;
                        }
                        else if (i == 2)
                        {
                            throw;
                        }
                    }
                }
                embed.WithDescription($"User {invokerFormatted} ({Context.User.Id})\nHas approved {user.Mention} ({targetFormatted}, {user.Id})")
                .WithColor(Color.Green);
            }
            try
            {
                await _service.SendGreeterMessage(Context.Guild, user);
            }
            catch (Exception ex)
            {
                _log.Warn(ex, $"Failed to send greeter message for user \"{user.Username}#{user.Discriminator}\" ({user.Id}) in Guild \"{Context.Guild.Name}\" ({Context.Guild.Id})");
            }
            await FollowupAsync(embed: embed.Build());
            return;
        }
        catch (Exception ex)
        {
            var errorType = $"{ex.GetType().Name}.{ex.GetType().Name}".Replace("`", "\\`");
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes($"Failed to approve user: {user.Username}#{user.Discriminator} ({user.Id})")
                .WithContext(Context)
                .WithUser(user));
            
            await FollowupAsync(embed:
                embed.WithDescription(string.Join("\n", "Failed to add event to channel.", $"`{errorType}`"))
                .WithColor(Color.Red)
                .Build());
        }
    }

}
