using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using SkidBot.Core.Controllers.BotAdditions;
using SkidBot.Core.Helpers;

namespace SkidBot.Core.Modules;

public class ReminderModule : InteractionModuleBase
{
    [SlashCommand("remind", "Create a reminder")]
    public async Task CreateReminder(string when, string? note = null)
    {
        var timeSpan = TimeHelper.ParseFromString(when);
        var timestamp = DateTimeOffset.UtcNow.Add(timeSpan).ToUnixTimeSeconds();
        var controller = Program.Services.GetRequiredService<ReminderController>();
        await controller.CreateReminderTask(
            timestamp,
            Context.User.Id,
            Context.Channel.Id,
            Context.Guild.Id,
            note);

        var embed = DiscordHelper.BaseEmbed()
            .WithDescription($"Reminder will be sent <t:{timestamp}:R>")
            .WithColor(Color.Green);
        await Context.Interaction.RespondAsync(embed: embed.Build());
    }
}