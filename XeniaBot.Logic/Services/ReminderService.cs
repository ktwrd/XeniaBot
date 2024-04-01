using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using kate.shared.Helpers;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Data.Repositories;
using XeniaBot.Data.Models;
using XeniaBot.Shared;
using XeniaBot.Shared.Helpers;
using XeniaBot.Shared.Services;
using Timer = System.Timers.Timer;

namespace XeniaBot.Logic.Services;

[XeniaController]
public class ReminderService : BaseService
{
    private readonly CoreContext _core;
    private readonly ConfigData _configData;
    private readonly DiscordSocketClient _discordClient;
    private readonly ReminderRepository _reminderDb;
    public ReminderService(IServiceProvider services)
        : base(services)
    {
        _core = services.GetRequiredService<CoreContext>();
        _configData = services.GetRequiredService<ConfigData>();
        _discordClient = services.GetRequiredService<DiscordSocketClient>();
        _reminderDb = services.GetRequiredService<ReminderRepository>();
        CurrentReminders = new List<string>();
    }
    
    /// <summary>
    /// Seconds since Unix Epoch (UTC)
    /// </summary>
    public long InitTimestamp { get; private set; }
    
    /// <summary>
    /// List of <see cref="ReminderModel.ReminderId"/> that has a timer created.
    /// </summary>
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

        CreateUnregisteredTasksCompleted += ReminderDbCheckLoop;
    }

    /// <summary>
    /// Runs function every 5 seconds to look call <see cref="AddReminderTask"/> on every reminder that isn't in <see cref="CurrentReminders"/>.
    /// </summary>
    private void ReminderDbCheckLoop()
    {
        new System.Threading.Timer(
            ReminderDbCheckLoop_Callback,
            null,
            0,
            5000);
    }
    private void ReminderDbCheckLoop_Callback(object? obj)
    {
        lock (CurrentReminders)
        {
            var cur = CurrentReminders.ToArray();
            var notCalled = _reminderDb.GetForgotten(cur, InitTimestamp).Result;
                    
            CreateUnregisteredTasks(cur).Wait();

            CurrentReminders = cur.Concat(notCalled.Select(v => v.ReminderId)).ToList();
        }
    }

    /// <summary>
    /// Call <see cref="SendNotification"/> for all reminders that are due to call that have <see cref="ReminderModel.HasReminded"/> set to `false`.
    /// </summary>
    private async Task CallForgottenReminders()
    {
        if (!_configData.ReminderService.Enable)
        {
            Log.Warn("Ignoring since ReminderServiceConfigItem.Enable is false");
            return;
        }
        var notCalled = await _reminderDb.GetMany(
            beforeTimestamp: InitTimestamp,
            hasReminded: false) ?? [];

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

        await CreateUnregisteredTasks(appendToCurrentReminders: true);
    }

    /// <summary>
    /// Call <see cref="AddReminderTask"/> for all reminders. Will ignore timer creation of timer if <see cref="ReminderModel.ReminderId"/> exists in <paramref name="ignoreItems"/>
    /// </summary>
    /// <param name="ignoreItems">Array of ReminderId that should be ignored when calling <see cref="AddReminderTask"/></param>
    /// <param name="appendToCurrentReminders">When `true`, it will add <see cref="ReminderModel.ReminderId"/> <see cref="CurrentReminders"/> if it decides to call <see cref="AddReminderTask"/></param>
    private async Task CreateUnregisteredTasks(string[]? ignoreItems = null, bool appendToCurrentReminders = false)
    {
        ignoreItems ??= Array.Empty<string>();
        var targets = await _reminderDb.GetMany(
            afterTimestamp: InitTimestamp,
            hasReminded: false) ?? [];

        var taskList = new List<Task>();
        foreach (var i in targets)
        {
            if (i.HasReminded)
                continue;
            if (ignoreItems.Length < 1 || ignoreItems.Contains(i.ReminderId))
            {
                Log.Debug($"Registered {i.ReminderId}");
                taskList.Add(new Task(delegate { AddReminderTask(i).Wait(); }));
                if (appendToCurrentReminders)
                {
                    lock (CurrentReminders)
                    { CurrentReminders.Add(i.ReminderId); }
                }
            }
        }
        await XeniaHelper.TaskWhenAll(taskList);
        CreateUnregisteredTasksCompleted?.Invoke();
    }

    private VoidDelegate CreateUnregisteredTasksCompleted;
    #endregion

    #region Reminder Creation
    /// <summary>
    /// Create timer for Reminder which will then call <see cref="SendNotification"/>.
    ///
    /// <see cref="ReminderModel.ReminderTimestamp"/> should be more than 3s into the future.
    /// </summary>
    /// <param name="model"></param>
    private async Task AddReminderTask(ReminderModel model)
    {  
        var currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var diff = model.ReminderTimestamp - currentTimestamp;
        if (diff < 3)
        {
            Log.Warn($"Reminder ${model.ReminderId} too short, ignoring.");
            return;
        }
        var timer = new Timer(diff * 1000);
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
    /// <summary>
    /// Create and add a reminder into the database. Also calls <see cref="AddReminderTask"/>
    /// </summary>
    /// <param name="timestamp">Timestamp when the reminder should be ran at. Seconds since Unix Epoch (UTC)</param>
    /// <param name="userId">Snowflake for user that this reminder is for</param>
    /// <param name="channelId">Channel Id that the user should be pinged in</param>
    /// <param name="guildId">Guild Id this reminder is for</param>
    /// <param name="notes">(Optional) notes that the user should be pinged with.</param>
    /// <param name="source">Where was the reminder created from (<see cref="RemindSource"/>)</param>
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
    
    /// <summary>
    /// Send notifications to user for their reminder.
    ///
    /// Will not send if <see cref="ReminderModel.HasReminded"/> is `true`.
    /// </summary>
    /// <param name="reminderId"><see cref="ReminderModel.ReminderId"/></param>
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
            .WithDescription(model.Note)
            .WithColor(Color.Blue);

        await channel.SendMessageAsync($"<@{model.UserId}>", embed: embed.Build());

        model.MarkAsComplete();
        await _reminderDb.Set(model);
    }
}