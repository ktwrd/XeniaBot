using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Core.Services.BotAdditions;
using XeniaBot.Core.Helpers;
using XeniaBot.Data.Models;
using XeniaBot.Logic.Services;

namespace XeniaBot.Core.Modules;

public class ReminderModule : InteractionModuleBase
{
    [SlashCommand("remind", "Create a reminder")]
    public async Task CreateReminder(
        [Discord.Interactions.Summary(description: "When you will be reminded. Example 2d 1hrs 5seconds")]
        string when, string? note = null)
    {
        TimeSpan timeSpan;
        try
        {
            timeSpan = TimeHelper.ParseFromString(when);
        }
        catch (Exception exception)
        {
            await RespondAsync(embed: new EmbedBuilder()
                .WithTitle("Failed to create Reminder")
                .WithDescription($"Failed to parse `when` parameter.\n`{exception.Message}`")
                .WithColor(Color.Red)
                .Build());
            await DiscordHelper.ReportError(exception, Context);
            return;
        }
        var timestamp = DateTimeOffset.UtcNow.Add(timeSpan).ToUnixTimeSeconds();
        try
        {
            var controller = Program.Core.GetRequiredService<ReminderService>();
            var currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var diff = timestamp - currentTimestamp;
            if (diff < 1)
            {
                await RespondAsync(embed: new EmbedBuilder()
                    .WithTitle($"Failed to create Reminder")
                    .WithDescription($"You can't set a reminder for the past!")
                    .WithColor(Color.Red)
                    .Build());
                return;
            }
            else if (diff < 3)
            {
                await RespondAsync(
                    embed: new EmbedBuilder()
                        .WithTitle($"Failed to create Reminder")
                        .WithDescription($"Reminder timestamp is too soon! Must be more than 3s into the future.")
                        .WithColor(Color.Red)
                        .Build());
                return;
            }

            await controller.CreateReminderTask(
                timestamp,
                Context.User.Id,
                Context.Channel.Id,
                Context.Guild.Id,
                note,
                RemindSource.Bot);
        }
        catch (Exception e)
        {
            await Context.Interaction.RespondAsync(embed: new EmbedBuilder()
                .WithTitle("Failed to create Reminder")
                .WithDescription($"Failed to create reminder.\n`{e.Message}`")
                .WithColor(Color.Red)
                .Build());
            await DiscordHelper.ReportError(e, Context);
            return;
        }

        var embed = DiscordHelper.BaseEmbed()
            .WithTitle("Reminder Created")
            .WithDescription($"Reminder will be sent <t:{timestamp}:R>")
            .AddField("Notes", $"```\n{note}\n```")
            .WithColor(Color.Green);
        await Context.Interaction.RespondAsync(embed: embed.Build());
    }
}