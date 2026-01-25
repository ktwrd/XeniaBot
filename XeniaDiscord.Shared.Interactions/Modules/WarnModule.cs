using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Shared.Helpers;
using XeniaDiscord.Common;
using XeniaDiscord.Common.Interfaces;
using XeniaDiscord.Data;

namespace XeniaDiscord.Shared.Interactions.Modules;

[CommandContextType(InteractionContextType.Guild)]
public class WarnModule : InteractionModuleBase
{
    private readonly IWarnService _warnService;
    public WarnModule(IServiceProvider services)
    {
        _warnService = services.GetRequiredService<IWarnService>();
    }

    [CommandContextType(InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
    [SlashCommand("warn", "Warn a user.")]
    [RequireUserPermission(GuildPermission.ModerateMembers)]
    public async Task CreateAsync(
        IUser targetUser,
        [MinLength(1)]
        [MaxLength(DbGlobals.MaxStringSize)]
        string reason)
    {
        await DeferAsync();
        IGuild? guild = null;
        try
        {
            await Task.Delay(50);
            guild = await Context.Client.GetGuildAsync(Context.Interaction.GuildId.GetValueOrDefault(0));
            if (guild == null)
            {
                await FollowupAsync("This command can only be ran in guilds.", ephemeral: true);
                return;
            }

            var result = await _warnService.CreateAsync(
                guild,
                targetUser,
                reason,
                Context.Interaction.User);

            var embed = new EmbedBuilder()
                .WithTitle("Create Warning")
                .WithCurrentTimestamp();
            switch (result.Kind)
            {
                case CreateWarnResultKind.Success:
                    embed.WithDescription($"Created warn for user: <@{targetUser.Id}> (`{targetUser.Username}`)")
                        .WithColor(Color.Blue);
                    if (result.Model?.Id != null)
                    {
                        var url = await _warnService.GetDashboardUrl(result.Model);
                        if (string.IsNullOrEmpty(url))
                        {
                            embed.Description += $"\n-# Warn Id: `{result.Model?.Id}`";
                        }
                        else
                        {
                            embed.Description += $"\n[View on Dashboard]({url})";
                            embed.WithUrl(url);
                        }
                    }
                    break;
                case CreateWarnResultKind.MissingPermissions:
                    embed.WithDescription("You do not have permission to create warns.")
                        .WithColor(Color.Red);
                    break;
                case CreateWarnResultKind.MissingReason:
                    embed.WithDescription("Missing warn reason.")
                        .WithColor(Color.Orange);
                    break;
                case CreateWarnResultKind.ReasonTooLong:
                    embed.WithDescription($"Reason must be less than {DbGlobals.MaxStringSize} characters. (is: {reason.Length})")
                        .WithColor(Color.Orange);
                    break;
            }
            if (!result.IsGuildConfigured && result.Kind != CreateWarnResultKind.MissingPermissions)
            {
                embed.Description += "\n\n-# ⚠️ Guild Config hasn't been setup. Use `/help warn` for more info.";
            }

            await FollowupAsync(embed: embed.Build());
        }
        catch (Exception ex)
        {
            var embed = new EmbedBuilder()
                .WithTitle("Create Warning - Error")
                .WithDescription(ex.Message[..Math.Min(1900, ex.Message.Length)])
                .WithColor(Color.Red)
                .WithCurrentTimestamp();
            if (SentrySdk.IsEnabled)
            {
                var id = SentrySdk.CaptureException(ex, scope =>
                {
                    scope.SetInteractionInfo(Context);

                    scope.SetExtra("param.targetUser.id", targetUser?.Id);
                    scope.SetExtra("param.targetUser.username", targetUser?.Username);
                    scope.SetExtra("param.targetUser.global_name", targetUser?.GlobalName);
                    scope.SetExtra("param.reason", reason);
                });
                embed.WithFooter(id.ToString());
            }
            await FollowupAsync(embed: embed.Build());
        }
    }
}
