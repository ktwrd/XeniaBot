using System.Threading.Tasks;
using Discord.Interactions;
using XeniaBot.Core.Helpers;

namespace XeniaBot.Core.Modules;

[Discord.Interactions.Group("help", "Usage help for commands and modules")]
public class HelpModule : InteractionModuleBase
{
    [SlashCommand("remind", "Usage for the reminder command")]
    public async Task Remind()
    {
        var embed = DiscordHelper.BaseEmbed()
            .WithTitle("Remind Module Help")
            .WithDescription(string.Join("\n\n", new string[]
            {
                "To use the reminder system, you can call it with the `/remind` command.",
                "For the `when` parameter it supports unordered day/hour/minute/second definition. So if you want to set a reminder for 3 days and 40min you can use the following command",
                "`/remind 3d 40m`"
            }));
        await Context.Interaction.RespondAsync(embed: embed.Build());
    }
}