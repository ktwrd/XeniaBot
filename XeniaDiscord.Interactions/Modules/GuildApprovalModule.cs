using Discord;
using Discord.Interactions;
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
            embed = await _service.ApproveUserEmbed(user, Context.User, Context);
            await FollowupAsync(embed: embed.Build());
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
