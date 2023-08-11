using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Core.Helpers;
using XeniaBot.Core.Models;
using XeniaBot.Shared;

namespace XeniaBot.Core.Controllers.BotAdditions;

[BotController]
public class ReminderController : BaseController
{
    private readonly ReminderConfigController _config;
    private readonly DiscordSocketClient _discord;
    public ReminderController(IServiceProvider services)
        : base(services)
    {
        _config = services.GetRequiredService<ReminderConfigController>();
        _discord = services.GetRequiredService<DiscordSocketClient>();
    }


    public override async Task OnReady()
    {
        InitTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        await CallForgottenReminders();
        await InitTasks();
    }

    private long InitTimestamp { get; set; }

    private async Task CallForgottenReminders()
    {
        var notCalled = await _config.GetMany(
            beforeTimestamp: InitTimestamp,
            hasReminded: false) ?? Array.Empty<ReminderModel>();

        var taskList = new List<Task>();
        foreach (var item in notCalled)
        {
            taskList.Add(new Task(delegate
            {
                SendNotification(item).Wait();
            }));
        }
        foreach (var i in taskList)
            i.Start();
        await Task.WhenAll(taskList);
    }

    private async Task InitTasks()
    {
        var targets = await _config.GetMany(
            afterTimestamp: InitTimestamp,
            hasReminded: false) ?? Array.Empty<ReminderModel>();

        var taskList = new List<Task>();
        foreach (var i in targets)
            taskList.Add(new Task(delegate { AddReminderTask(i).Wait(); }));
        await SGeneralHelper.TaskWhenAll(taskList);
    }

    private async Task AddReminderTask(ReminderModel model)
    {
        var currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var diff = (model.ReminderTimestamp - currentTimestamp) * 1000;
        var timer = new Timer(diff);
        timer.Elapsed += (sender, args) =>
        {
            var data = _config.Get(model.ReminderId).Result;
            SendNotification(data).Wait();
        };
        timer.Enabled = true;
        timer.AutoReset = false;
        timer.Start();
    }

    public async Task CreateReminderTask(
        long timestamp,
        ulong userId,
        ulong channelId,
        ulong guildId,
        string? notes)
    {
        var model = new ReminderModel(
            userId,
            channelId,
            guildId,
            timestamp,
            notes);
        await _config.Set(model);
        await AddReminderTask(model);
    }
    
    private async Task SendNotification(ReminderModel model)
    {
        if (model == null)
            return;
        var guild = _discord.GetGuild(model.GuildId);
        var channel = guild.GetTextChannel(model.ChannelId);

        var embed = DiscordHelper.BaseEmbed()
            .WithTitle("Reminder")
            .WithDescription(model.Note);

        await channel.SendMessageAsync($"<@{model.UserId}>", embed: embed.Build());

        model.HasReminded = true;
        await _config.Set(model);
    }
}