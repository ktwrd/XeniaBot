using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using Sentry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using XeniaBot.MongoData.Models;
using XeniaBot.MongoData.Repositories;
using XeniaBot.Shared;
using XeniaBot.Shared.Helpers;

namespace XeniaBot.Logic.Services;

[XeniaController]
public class ReminderService : BaseService
{
    private readonly Logger _log = LogManager.GetLogger("Xenia." + nameof(ReminderService));
    private readonly ConfigData _configData;
    private readonly DiscordSocketClient _discordClient;
    private readonly ReminderRepository _reminderDb;
    public ReminderService(IServiceProvider services)
        : base(services)
    {
        _configData = services.GetRequiredService<ConfigData>();
        _discordClient = services.GetRequiredService<DiscordSocketClient>();
        _reminderDb = services.GetRequiredService<ReminderRepository>();
        CurrentReminders = new List<string>();
    }
    
    /// <summary>
    /// Unix Timestamp (Seconds, UTC)
    /// </summary>
    public long InitTimestamp { get; private set; }
    
    /// <summary>
    /// List of <see cref="ReminderModel.ReminderId"/> that has a timer created.
    /// </summary>
    private List<string> CurrentReminders { get; set; }

    private readonly SemaphoreSlim _currentRemindersLock = new(1, 1);

    #region OnReady
    public override async Task OnReady()
    {
        InitTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (!_configData.ReminderService.Enable)
        {
            _log.Info("Ignoring since ReminderServiceConfigItem.Enable is false");
            return;
        }

        try
        {
            await CallForgottenReminders();
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            _log.Error(ex, $"Failed to call {nameof(CallForgottenReminders)}");
            // TODO submit error to ErrorReportingService
        }
        try
        {
            await OnReadyTasks();
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            _log.Error(ex, $"Failed to call {nameof(OnReadyTasks)}");
            // TODO submit error to ErrorReportingService
        }
    }

    private void ReminderDatabaseThread()
    {
        try
        {
            ReminderDatabaseThreadLoop().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to call thread loop");
            // TODO submit error to ErrorReportingService
        }
        CreateReminderDatabaseThread();
    }

    private void CreateReminderDatabaseThread()
    {
        new Thread(ReminderDatabaseThread)
        {
            Name = $"{nameof(ReminderService)}.{nameof(ReminderDatabaseThread)}"
        }.Start();
    }

    /// <summary>
    /// Runs function every 5 seconds to look call <see cref="AddReminderTask"/> on every reminder that isn't in <see cref="CurrentReminders"/>.
    /// </summary>
    private async Task ReminderDatabaseThreadLoop()
    {
        while (true)
        {
            List<ReminderModel> notCalled = [];
            await _currentRemindersLock.WaitAsync();
            try
            {
                var cur = CurrentReminders.ToArray();
                notCalled = await _reminderDb.GetForgotten(cur, InitTimestamp);

                CreateUnregisteredTasks(cur).Wait();

                CurrentReminders = cur.Concat(notCalled.Select(v => v.ReminderId)).ToList();
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex, scope =>
                {
                    scope.SetExtra(nameof(CurrentReminders), string.Join(", ", CurrentReminders));
                    scope.SetExtra(nameof(notCalled), notCalled);
                });
                _log.Error(ex, $"Failed to run {nameof(CreateUnregisteredTasks)}");
            }
            finally
            {
                _currentRemindersLock.Release();
            }

            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }

    /// <summary>
    /// Call <see cref="SendNotification"/> for all reminders that are due to call that have <see cref="ReminderModel.HasReminded"/> set to `false`.
    /// </summary>
    private async Task CallForgottenReminders()
    {
        if (!_configData.ReminderService.Enable)
        {
            _log.Debug("Ignoring since ReminderServiceConfigItem.Enable is false");
            return;
        }
        try
        {
            var notCalled = await _reminderDb.GetMany(
                beforeTimestamp: InitTimestamp,
                hasReminded: false) ?? [];

            var taskList = new List<Task>();
            foreach (var item in notCalled.Where(e => !e.HasReminded).Select(e => e.ReminderId))
            {
                var reminderId = item;
                _log.Debug($"Called {reminderId}");
                taskList.Add(new Task(delegate
                {
                    SendNotification(reminderId).GetAwaiter().GetResult();
                }));
            }
            foreach (var i in taskList)
                i.Start();
            await Task.WhenAll(taskList);
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to send notifications for forgotten reminders");
            SentrySdk.CaptureException(ex);
        }
    }
    private async Task OnReadyTasks()
    {
        if (!_configData.ReminderService.Enable)
        {
            _log.Debug("Ignoring since ReminderServiceConfigItem.Enable is false");
            return;
        }

        try
        {
            await CreateUnregisteredTasks(appendToCurrentReminders: true);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            _log.Error(ex, $"Failed to call {nameof(CreateUnregisteredTasks)}");
        }

        try
        {
            CreateReminderDatabaseThread();
        }
        catch (Exception ex)
        {
            _log.Fatal(ex, "Failed to create thread loop for processing reminders!");
        }
    }

    /// <summary>
    /// Call <see cref="AddReminderTask"/> for all reminders. Will ignore timer creation of timer if <see cref="ReminderModel.ReminderId"/> exists in <paramref name="ignoreItems"/>
    /// </summary>
    /// <param name="ignoreItems">Array of ReminderId that should be ignored when calling <see cref="AddReminderTask"/></param>
    /// <param name="appendToCurrentReminders">When `true`, it will add <see cref="ReminderModel.ReminderId"/> <see cref="CurrentReminders"/> if it decides to call <see cref="AddReminderTask"/></param>
    private async Task CreateUnregisteredTasks(IReadOnlyCollection<string>? ignoreItems = null, bool appendToCurrentReminders = false)
    {
        ignoreItems ??= Array.Empty<string>();
        var targets = await _reminderDb.GetMany(
            afterTimestamp: InitTimestamp,
            hasReminded: false) ?? [];

        var taskList = new List<Task>();
        var appendReminders = new HashSet<string>();
        await _currentRemindersLock.WaitAsync();
        try
        {
            foreach (var i in targets.Where(e => !e.HasReminded))
            {
                if (ignoreItems.Count >= 1 && !ignoreItems.Contains(i.ReminderId)) continue;
                _log.Debug($"Registered {i.ReminderId}");
                var item = i;
                taskList.Add(new Task(delegate { AddReminderTask(item).GetAwaiter().GetResult(); }));
                if (appendToCurrentReminders)
                {
                    appendReminders.Add(i.ReminderId);
                }
            }
            CurrentReminders.AddRange(appendReminders);
        }
        finally
        {
            _currentRemindersLock.Release();
        }
        await XeniaHelper.TaskWhenAll(taskList);
    }
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
            _log.Warn($"Reminder ${model.ReminderId} too short, running now");
            await SendNotification(model.ReminderId);
            return;
        }

        var targetTime = DateTimeOffset.FromUnixTimeSeconds(model.ReminderTimestamp);
        var reminderId = model.ReminderId;
        new Thread(() =>
        {
            try
            {
                var now = DateTimeOffset.UtcNow;
                _log.Info($"Started thread for reminder {reminderId} (target: {targetTime}, which is in {targetTime - DateTimeOffset.UtcNow})");
                while (now < targetTime)
                {
                    if (now - targetTime > ReminderBigDelayCheck)
                    {
                        Task.Delay(ReminderBigDelay).Wait();
                    }
                    Task.Delay(1_000).Wait();
                    now = DateTimeOffset.UtcNow;
                }
                _log.Trace($"Triggering event: {reminderId}");
                var data = _reminderDb.Get(reminderId).GetAwaiter().GetResult();
                if (data != null)
                {
                    SendNotification(data.ReminderId).GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to send reminder for {reminderId}");
            }
        })
        {
            Name = $"{nameof(ReminderService)}.{nameof(AddReminderTask)}({nameof(model.ReminderId)}={reminderId})"
        }.Start();
    }

    private static TimeSpan ReminderBigDelayCheck => TimeSpan.FromHours(6);
    private static TimeSpan ReminderBigDelay => TimeSpan.FromHours(5);

    /// <summary>
    /// Create and add a reminder into the database. Also calls <see cref="AddReminderTask"/>
    /// </summary>
    /// <param name="timestamp">Timestamp when the reminder should be run at. Seconds since Unix Epoch (UTC)</param>
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
        try
        {
            var model = await _reminderDb.Get(reminderId);
            if (model == null)
                return;
            if (model.HasReminded)
                return;
            
            var channel = await ExceptionHelper.RetryOnTimedOut(async () => await _discordClient.GetChannelAsync(model.ChannelId));
            if (channel is not ITextChannel textChannel)
            {
                _log.Error($"Channel for Reminder {reminderId} isn't a text channel");
                return;
            }

            var embed = XeniaHelper.BaseEmbed()
                .WithTitle("Reminder")
                .WithDescription(model.Note)
                .WithColor(Color.Blue);
            if (!string.IsNullOrEmpty(model.Note?.Trim()))
            {
                embed.Description = model.Note;
            }

            await ExceptionHelper.RetryOnTimedOut(async () => await textChannel.SendMessageAsync($"<@{model.UserId}>", embed: embed.Build()));

            model.MarkAsComplete();
            await _reminderDb.Set(model);
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to send reminder {reminderId}");
        }
    }
}