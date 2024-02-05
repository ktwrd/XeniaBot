using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Core.Services.BotAdditions;
using XeniaBot.Core.Helpers;
using XeniaBot.Data.Models;
using XeniaBot.Data.Repositories;

namespace XeniaBot.Core.Modules;

[Group("economy", "Economy module. Money and stuff!")]
public class EconomyModule : InteractionModuleBase
{
    [SlashCommand("daily", "Get daily reward")]
    public async Task Daily()
    {
        var embed = new EmbedBuilder()
            .WithTitle("Economy - Daily")
            .WithCurrentTimestamp();
        var controller = Program.Core.GetRequiredService<EconomyProfileRepository>();

        EconProfileModel? data = null;
        try
        {
            if (controller == null)
                throw new Exception("EconomyProfileRepository is null");
            data = await controller.Get(Context.User.Id, Context.Guild.Id)
               ?? new EconProfileModel()
               {
                   UserId = Context.User.Id,
                   GuildId = Context.Guild.Id,
                   LastDailyTimestamp = 0
               };
            if (data == null)
            {
                throw new Exception("Fetched data is null");
            }
        }
        catch (Exception ex)
        {
            embed.WithDescription($"Failed to read data!\n```\n{ex.Message}\n```")
                .WithColor(Color.Red);
            await Context.Interaction.RespondAsync(embed: embed.Build(), ephemeral: true);
            await DiscordHelper.ReportError(ex, Context);
            return;
        }
        
        // Check if last daily was more than 24hr ago.
        // If is was less than a day ago then we deny access to the user.
        var currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var timestampDiff = currentTimestamp - data.LastDailyTimestamp;
        if (timestampDiff < 86400)
        {
            var internalDiff = 86400 - timestampDiff;
            var second = Math.Round(internalDiff % 60f);
            var minute = Math.Round((internalDiff / 60f) % 60f);
            var hour = Math.Floor(minute % 60);
            var timeStr = "";
            if (hour > 0)
                timeStr += $"{hour} hour" + (hour > 1 ? "s " : " ");
            if (minute > 0)
                timeStr += $"{minute} minute" + (minute > 1 ? "s " : " ");
            if (second > 0)
                timeStr += $"{second} second" + (second > 1 ? "s" : "");
            embed.WithDescription($"Too fast! Try again in {timeStr}")
                .WithColor(Color.Orange);
            await Context.Interaction.RespondAsync(embed: embed.Build(), ephemeral: true);
            return;
        }

        try
        {
            // Increment coins and set last timestamp
            var inc = new Random().Next(10, 30);
            data.Coins += inc;
            data.LastDailyTimestamp = currentTimestamp;
            await controller.Set(data);

            embed.WithDescription($"You gained `{inc}` coins!")
                .AddField("Current Balance", $"`{data.Coins} coins`");
            await Context.Interaction.RespondAsync(embed: embed.Build(), ephemeral: true);
        }
        catch (Exception ex)
        {
            embed.WithDescription($"Failed to save data!\n```\n{ex.Message}\n```")
                .WithColor(Color.Red);
            await Context.Interaction.RespondAsync(embed: embed.Build(), ephemeral: true);
            await DiscordHelper.ReportError(ex, Context);
            return;
        }
    }

    [SlashCommand("balance", "Fetch your balance")]
    public async Task Balance()
    {
        var embed = new EmbedBuilder()
            .WithTitle("Economy - Balance")
            .WithCurrentTimestamp();
        var controller = Program.Core.GetRequiredService<EconomyProfileRepository>();

        try
        {
            if (controller == null)
                throw new Exception("EconomyProfileRepository is null");

            var data = await controller.Get(Context.User.Id, Context.Guild.Id)
               ?? new EconProfileModel()
               {
                   UserId = Context.User.Id,
                   GuildId = Context.Guild.Id,
                   LastDailyTimestamp = 0
               };
                       
            embed.WithDescription($"Balance: `{data.Coins} coins`");
            await Context.Interaction.RespondAsync(embed: embed.Build(), ephemeral: true);
        }
        catch (Exception ex)
        {
            embed.WithDescription($"Failed to read data!\n```\n{ex.Message}\n```")
                .WithColor(Color.Red);
            await Context.Interaction.RespondAsync(embed: embed.Build(), ephemeral: true);
            await DiscordHelper.ReportError(ex, Context);
        }
    }
}