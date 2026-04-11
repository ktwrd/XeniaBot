using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Shared;
using XeniaBot.Shared.Services;
using XeniaDiscord.Common.Services;

namespace XeniaDiscord.Interactions.Modules;

[CommandContextType(InteractionContextType.Guild)]
public class GuildApprovalModule : InteractionModuleBase
{
    private readonly ErrorReportService _err;
    private readonly GuildApprovalService _service;
    public GuildApprovalModule(IServiceProvider services)
    {
        _err = services.GetRequiredService<ErrorReportService>();
        _service = services.GetRequiredService<GuildApprovalService>();
    }

    [SlashCommand("approve", "Guild Approvals: Approve a user")]
    [RequireUserPermission(GuildPermission.ManageRoles)]
    [RequireBotPermission(GuildPermission.ManageRoles)]
    [RegisterDBLCommand]
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
