using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;

namespace XeniaBot.Core.Modules;

public class DiceModule : InteractionModuleBase
{
    [SlashCommand("dice", "Throw a dice. Defaults to a 6-sided dice")]
    public async Task Dice(
        [Discord.Interactions.Summary(description: "Maximum value for dice roll")]
        int max, 
        [Discord.Interactions.Summary(description: "Minimum value for dice roll. Default: 1")]
        int min = 1)
    {
        var result = new Random().Next(min, max);
        var embed = new EmbedBuilder()
            .WithTitle("Dice")
            .WithDescription($"Resulted in `{result}` (between {min} and {max})")
            .WithColor(Color.Blue);
        await RespondAsync(embed: embed.Build());
    }
}