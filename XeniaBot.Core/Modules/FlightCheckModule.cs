using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Core.Controllers.BotAdditions;
using XeniaBot.Core.Helpers;

namespace XeniaBot.Core.Modules;

[Group("flightcheck", "Run FlightCheck tasks")]
public class FlightCheckModule : InteractionModuleBase
{
    [SlashCommand("run", "Run a FlightCheck on this guild.")]
    public async Task Run()
    {
        var embed = new EmbedBuilder()
            .WithTitle("FlightCheck - Run")
            .WithCurrentTimestamp();
        await Context.Interaction.DeferAsync();
        try
        {
            var controller = Program.Services.GetRequiredService<FlightCheckController>();
            if (Context.Guild == null)
            {
                await RespondAsync(
                    embed: embed
                        .WithColor(Color.Red)
                        .WithDescription("This command can only be ran in guilds").Build());
                return;
            }

            await controller.RunGuildFlightCheck(Context.Guild);
        }
        catch (Exception ex)
        {
            await RespondAsync(embed: embed.WithColor(Color.Red)
                .WithDescription(string.Join("\n", new string[]
                {
                    "Unable to perform FlightCheck",
                    "```",
                    ex.Message,
                    "```"
                })).Build());
            await DiscordHelper.ReportError(ex, Context);
            return;
        }

        await RespondAsync(
            embed: embed.WithColor(Color.Blue)
                .WithDescription(
                    "FlightCheck ran successfully. Ask the guild owner to check their DMs.\n" +
                    "If the server owner received no DMs, then Xenia is configured correctly.")
                .Build());
    }
}