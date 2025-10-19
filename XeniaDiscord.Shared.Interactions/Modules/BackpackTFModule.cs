using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System.Text;
using XeniaBot.Common.Services;
using XeniaBot.Shared;
using DColor = Discord.Color;

namespace XeniaDiscord.Shared.Interactions.Modules;

[Group("tf2", "Backpack.tf Integration")]
public class BackpackTFModule : InteractionModuleBase
{
    public BackpackTFModule(IServiceProvider services)
    {
        _backpackTf = services.GetRequiredService<BackpackTFService>();
        _programDetails = services.GetRequiredService<ProgramDetails>();
    }
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly BackpackTFService _backpackTf;
    private readonly ProgramDetails _programDetails;

    [SlashCommand("currency", "List currencies and their current worth")]
    public async Task GetCurrencyPlural()
    {
        await DeferAsync();
        var embed = new EmbedBuilder()
            .WithAuthor("Backpack.tf - Currencies", "https://res.kate.pet/upload/backpacktf-icon.png")
            .WithColor(new DColor(91, 105, 129))
            .WithCurrentTimestamp();
        try
        {
            var data = await _backpackTf.GetCurrenciesAsync();
            if (data == null)
            {
                embed.WithDescription($"Failed to fetch currency data!!")
                    .WithColor(DColor.Red);
                await Context.Interaction.FollowupAsync(embed: embed.Build());
                return;
            }

            foreach (var item in data.Items)
            {
                decimal refinedCost = data.GetRefinedCost(item.Price);
                decimal keyCost = data.GetKeyCost(item.Price);
                decimal dollarCost = data.GetDollarCost(item.Price);

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
            _log.Error(ex, $"Failed to get currencies via {_backpackTf.GetType()}");
            SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetInteractionInfo(Context);
            });
            embed.Fields.Clear();
            embed.WithDescription($"Failed to get currency data!")
                .AddField("Error Message", ex.Message[..2000])
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