using Discord;
using Discord.Interactions;
using JetBrains.Annotations;
using System;
using System.Threading.Tasks;
using XeniaBot.Core.Helpers;
using XeniaBot.Logic.Services;
using XeniaBot.MongoData.Models;
using XeniaBot.Shared;
using XeniaBot.Shared.Helpers;

namespace XeniaBot.Core.Modules;

public class ReminderModule : InteractionModuleBase
{
    [SlashCommand("remind", "Create a reminder")]
    [RegisterDBLCommand]
    [UsedImplicitly]
    public async Task CreateReminder(
        [Summary(description: "When you will be reminded. Example 2d 1hrs 5seconds")]
        string when, string? note = null)
    {
        TimeSpan timeSpan;
        try
        {
            timeSpan = TimeHelper.ParseFromString(when);
        }
        catch (Exception e)
        {
            var msg = e.Message.Length > 3000 ? e.Message[..3000] + "..." : e.Message;
            msg = msg.Replace("`", "");
            var embedErr = new EmbedBuilder()
                .WithTitle("Failed to create Reminder")
                .WithDescription($"Failed to parse `when` parameter.\n```\n{msg}\n```")
                .WithColor(Color.Red);
            await ExceptionHelper.RetryOnTimedOut(async () => await RespondAsync(embed: embedErr.Build()));
            await DiscordHelper.ReportError(e, Context);
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
                var embedErr = new EmbedBuilder()
                    .WithTitle("Failed to create Reminder")
                    .WithDescription("You can't set a reminder for the past!")
                    .WithColor(Color.Red);
                await ExceptionHelper.RetryOnTimedOut(async () => await RespondAsync(embed: embedErr.Build()));
                return;
            }
            else if (diff < 3)
            {
                var embedErr = new EmbedBuilder()
                    .WithTitle($"Failed to create Reminder")
                    .WithDescription($"Reminder timestamp is too soon! Must be more than 3s into the future.")
                    .WithColor(Color.Red);
                await ExceptionHelper.RetryOnTimedOut(async () => await RespondAsync(embed: embedErr.Build()));
                return;
            }

            await controller.CreateReminderTask(
                timestamp,
                Context.User.Id,
                Context.Channel.Id,
                Context.Guild?.Id ?? 0,
                note,
                RemindSource.Bot);
        }
        catch (Exception e)
        {
            var msg = e.Message.Length > 3000 ? e.Message[..3000] + "..." : e.Message;
            msg = msg.Replace("`", "");
            var embedErr = new EmbedBuilder()
                .WithTitle("Failed to create Reminder")
                .WithDescription($"Failed to create reminder.\n`{msg}`")
                .WithColor(Color.Red);
            await ExceptionHelper.RetryOnTimedOut(async () => await RespondAsync(embed: embedErr.Build()));
            await DiscordHelper.ReportError(e, Context);
            return;
        }

        var embed = DiscordHelper.BaseEmbed()
            .WithTitle("Reminder Created")
            .WithDescription($"Reminder will be sent <t:{timestamp}:R>")
            .AddField("Notes", $"```\n{note}\n```")
            .WithColor(Color.Green);
        await ExceptionHelper.RetryOnTimedOut(async () => await RespondAsync(embed: embed.Build()));
    }
}