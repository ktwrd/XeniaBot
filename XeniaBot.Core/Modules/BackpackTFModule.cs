using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using XeniaBot.Core.Services.Wrappers;
using XeniaBot.Shared;

using DColor = Discord.Color;

namespace XeniaBot.Core.Modules;

[Group("tf2", "Backpack.tf Integration")]
public class BackpackTFModule : InteractionModuleBase
{
    [SlashCommand("currency", "List currencies and their current worth")]
    public async Task GetCurrencyPlural()
    {
        await Context.Interaction.DeferAsync();
        var embed = new EmbedBuilder()
            .WithAuthor("Backpack.tf - Currencies", "https://res.kate.pet/upload/backpacktf-icon.png")
            .WithColor(new DColor(91, 105, 129))
            .WithCurrentTimestamp();
        try
        {
            var controller = Program.Core.GetRequiredService<BackpackTFService>();
            var data = await controller.GetCurrenciesAsync();
            if (data == null)
            {
                embed.WithDescription($"Failed to fetch currency data!!")
                    .WithColor(DColor.Red);
                await Context.Interaction.FollowupAsync(embed: embed.Build());
                return;
            }

            foreach (var item in data)
            {
                decimal refinedCost = controller.GetRefinedCost(item.Price);
                decimal keyCost = controller.GetKeyCost(item.Price);
                decimal dollarCost = controller.GetDollarCost(item.Price);

                embed.AddField(item.Name, string.Join("\n",
                    item.Price.ToString(),
                    "Calculated: `" + string.Join(" / ", new string[]
                    {
                        $"{Math.Round(refinedCost, 2)}ref",
                        $"{Math.Round(keyCost, 2)}key",
                        $"US${Math.Round(dollarCost, 2)}"
                    }.Select(v => v.PadRight(8, ' '))) + "`",
                    $"Last updated: <t:{item.Price.LastUpdateTimestamp}:R>"));
            }

            await Context.Interaction.FollowupAsync(embed: embed.Build());
        }
        catch (Exception ex)
        {
            embed.Fields.Clear();
            embed.WithDescription($"Failed to get currency data! `{ex.Message}`")
                .WithColor(DColor.Red);
            
            var programDetails = Program.Core.GetRequiredService<ProgramDetails>();
            if (programDetails.Debug)
            {
                using var ms = new MemoryStream(Encoding.UTF8.GetBytes(ex.ToString()));
                await Context.Interaction.FollowupWithFileAsync(
                    embed: embed.Build(), fileStream: ms, fileName: "exception.txt");
            }
            else
            {
                await Context.Interaction.FollowupAsync(embed: embed.Build());
            }
        }
    }
}