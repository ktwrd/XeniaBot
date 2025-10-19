using System.Threading.Tasks;
using Discord.Interactions;
using XeniaBot.Core.Helpers;

namespace XeniaBot.Core.Modules;

[Group("help", "Usage help for commands and modules")]
public class HelpModule : InteractionModuleBase
{
    [SlashCommand("remind", "Usage for the reminder command")]
    public async Task Remind()
    {
        var embed = DiscordHelper.BaseEmbed()
            .WithTitle("Help - Reminders")
            .WithDescription(string.Join("\n\n",
                "To use the reminder system, you can call it with the `/remind` command.",
                "For the `when` parameter it supports unordered day/hour/minute/second definition. So if you want to set a reminder for 3 days and 40min you can use the following command",
                "`/remind 3d 40m`"
            ));
        await Context.Interaction.RespondAsync(embed: embed.Build());
    }

    [SlashCommand("bansync", "Usage for the Ban Sync module")]
    public async Task BanSync()
    {
        var embed = DiscordHelper.BaseEmbed()
            .WithTitle("Help - Ban Sync")
            .WithDescription(string.Join("\n\n",
                "The Ban Sync module is used to notify other server operators when a member was banned in your server (only if that member is also in the other peoples servers).",
                "For more information, check out the [Ban Sync Guide](https://xenia.kate.pet/guide/about_bansync)."));
        await Context.Interaction.RespondAsync(embed: embed.Build());
    }
}