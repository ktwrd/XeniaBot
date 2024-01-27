using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Core.Controllers.BotAdditions;
using XeniaBot.Core.Helpers;

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
            await Context.Interaction.RespondAsync(string.Join("\n", new string[]
            {
                "Failed to parse parameter \"when\"",
                "```",
                exception.Message,
                "```"
            }));
            await DiscordHelper.ReportError(exception, Context);
            return;
        }
        var timestamp = DateTimeOffset.UtcNow.Add(timeSpan).ToUnixTimeSeconds();
        try
        {
            var controller = Program.Core.GetRequiredService<ReminderController>();
            var currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var diff = (timestamp - currentTimestamp) * 1000;
            if (diff < 3)
            {
                await Context.Interaction.RespondAsync("Duration provided is too short! (must be <3s)");
                return;
            }
            await controller.CreateReminderTask(
                timestamp,
                Context.User.Id,
                Context.Channel.Id,
                Context.Guild.Id,
                note);
        }
        catch (Exception e)
        {
            await Context.Interaction.RespondAsync(string.Join("\n", new string[]
            {
                "Failed to create reminder",
                "```",
                e.ToString(),
                "```"
            }));
            await DiscordHelper.ReportError(e, Context);
            return;
        }

        var embed = DiscordHelper.BaseEmbed()
            .WithDescription($"Reminder will be sent <t:{timestamp}:R>")
            .WithColor(Color.Green);
        await Context.Interaction.RespondAsync(embed: embed.Build());
    }
}