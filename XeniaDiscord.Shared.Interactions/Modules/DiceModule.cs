using Discord;
using Discord.Interactions;

namespace XeniaDiscord.Shared.Interactions.Modules;

public class DiceModule : InteractionModuleBase
{
    [SlashCommand("dice", "Throw a dice. Defaults to a 6-sided dice")]
    public async Task Dice(
        [Summary(description: "Maximum value for dice roll")]
        int max, 
        [Summary(description: "Minimum value for dice roll. Default: 1")]
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