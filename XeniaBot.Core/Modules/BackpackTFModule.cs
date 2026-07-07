using Discord;
using Discord.Interactions;
using JetBrains.Annotations;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Core.Services.Wrappers;
using XeniaBot.Shared;
using DColor = Discord.Color;

namespace XeniaBot.Core.Modules;

[Group("tf2", "Backpack.tf Integration")]
public class BackpackTFModule : InteractionModuleBase
{
    private readonly BackpackTFService _service;
    private readonly ProgramDetails _programDetails;

    public BackpackTFModule(IServiceProvider services)
    {
        _service = services.GetRequiredService<BackpackTFService>();
        _programDetails = services.GetRequiredService<ProgramDetails>();
    }

    [SlashCommand("currency", "List currencies and their current worth")]
    [RegisterDBLCommand]
    [UsedImplicitly]
    public async Task GetCurrencyPlural()
    {
        await Context.Interaction.DeferAsync();
        var embed = new EmbedBuilder()
            .WithAuthor("Backpack.tf - Currencies", "https://res.kate.pet/upload/backpacktf-icon.png")
            .WithColor(new DColor(91, 105, 129))
            .WithCurrentTimestamp();
        try
        {
            var data = await _service.GetCurrenciesAsync();
            if (data == null)
            {
                embed.WithDescription($"Failed to fetch currency data!!")
                    .WithColor(DColor.Red);
                await Context.Interaction.FollowupAsync(embed: embed.Build());
                return;
            }

            foreach (var item in data)
            {
                var refinedCost = _service.GetRefinedCost(item.Price);
                var keyCost = _service.GetKeyCost(item.Price);
                var dollarCost = _service.GetDollarCost(item.Price);

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
            
            if (_programDetails.Debug)
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