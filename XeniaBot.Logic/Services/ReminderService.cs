using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Data.Controllers.BotAdditions;
using XeniaBot.Data.Models;
using XeniaBot.Shared;
using XeniaBot.Shared.Controllers;
using XeniaBot.Shared.Helpers;

using Timer = System.Timers.Timer;

namespace XeniaBot.Logic.Services;

[XeniaController]
public class ReminderService : BaseController
{
    private readonly CoreContext _core;
    private readonly ConfigData _configData;
    private readonly DiscordSocketClient _discordClient;
    private readonly ReminderConfigController _reminderDb;
    public ReminderService(IServiceProvider services)
        : base(services)
    {
        _core = services.GetRequiredService<CoreContext>();
        _configData = services.GetRequiredService<ConfigData>();
        _discordClient = services.GetRequiredService<DiscordSocketClient>();
        _reminderDb = services.GetRequiredService<ReminderConfigController>();
        CurrentReminders = new List<string>();
    }
    
    public long InitTimestamp { get; private set; }
    
    
    private List<string> CurrentReminders { get; set; }
    #region OnReady
    public override async Task OnReady()
    {
        InitTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (!_configData.ReminderService.Enable)
        {
            Log.Warn("Ignoring since ReminderServiceConfigItem.Enable is false");
            return;
        }

        await CallForgottenReminders();
        await OnReadyTasks();

        ReminderDbCheckLoop();
    }

    private async Task ReminderDbCheckLoop()
    {
        var timer = new System.Threading.Timer(
            (o) =>
            {
                lock (CurrentReminders)
                {
                    var cur = CurrentReminders.ToArray();
                    var notCalled = _reminderDb.GetForgotten(cur, InitTimestamp).Result;
                    
                    CreateUnregisteredTasks(cur).Wait();

                    CurrentReminders = cur.Concat(notCalled.Select(v => v.ReminderId)).ToList();
                }
            },
            null,
            0,
            5000);
    }

    private async Task CallForgottenReminders()
    {
        if (!_configData.ReminderService.Enable)
        {
            Log.Warn("Ignoring since ReminderServiceConfigItem.Enable is false");
            return;
        }
        var notCalled = await _reminderDb.GetMany(
            beforeTimestamp: InitTimestamp,
            hasReminded: false) ?? Array.Empty<ReminderModel>();

        var taskList = new List<Task>();
        foreach (var item in notCalled)
        {
            if (item.HasReminded)
                continue;
            Log.Debug($"Called {item.ReminderId}");
            taskList.Add(new Task(delegate
            {
                SendNotification(item.ReminderId).Wait();
            }));
        }
        foreach (var i in taskList)
            i.Start();
        await Task.WhenAll(taskList);
    }
    private async Task OnReadyTasks()
    {
        if (!_configData.ReminderService.Enable)
        {
            Log.Warn("Ignoring since ReminderServiceConfigItem.Enable is false");
            return;
        }

        await CreateUnregisteredTasks(Array.Empty<string>());
    }

    private async Task CreateUnregisteredTasks(string[] ignoreItems)
    {
        var targets = await _reminderDb.GetMany(
            afterTimestamp: InitTimestamp,
            hasReminded: false) ?? Array.Empty<ReminderModel>();

        var taskList = new List<Task>();
        foreach (var i in targets)
        {
            if (ignoreItems.Length > 0 && ignoreItems.Contains(i.ReminderId))
            {
                Log.Debug($"Registered {i.ReminderId}");
                taskList.Add(new Task(delegate { AddReminderTask(i).Wait(); }));
            }
        }
        await XeniaHelper.TaskWhenAll(taskList);
    }
    #endregion

    #region Reminder Creation
    private async Task AddReminderTask(ReminderModel model)
    {  
        var currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var diff = (model.ReminderTimestamp - currentTimestamp) * 1000;
        if (diff < 1)
        {
            Log.Warn($"Reminder ${model.ReminderId} too short, ignoring.");
            return;
        }
        var timer = new Timer(diff);
        timer.Elapsed += (sender, args) =>
        {
            var data = _reminderDb.Get(model.ReminderId).Result;
            if (data != null)
            {
                SendNotification(data.ReminderId).Wait();
            }
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
        string? notes,
        RemindSource source)
    {
        var model = new ReminderModel(
            userId,
            channelId,
            guildId,
            timestamp,
            source,
            notes);
        await _reminderDb.Set(model);
        await AddReminderTask(model);
    }
    #endregion
    
    private async Task SendNotification(string reminderId)
    {
        var model = await _reminderDb.Get(reminderId);
        if (model == null)
            return;
        if (model.HasReminded)
            return;
        
        var guild = _discordClient.GetGuild(model.GuildId);
        var channel = guild.GetTextChannel(model.ChannelId);

        var embed = XeniaHelper.BaseEmbed()
            .WithTitle("Reminder")
            .WithDescription(model.Note);

        await channel.SendMessageAsync($"<@{model.UserId}>", embed: embed.Build());

        model.HasReminded = true;
        model.RemindedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await _reminderDb.Set(model);
    }
}