using System.Timers;
using Discord;
using Discord.Rest;
using XeniaBot.Data.Moderation.Models;
using XeniaBot.Moderation.Helpers;
using XeniaBot.Shared;
using XeniaBot.Shared.Helpers;
using XeniaBot.Shared.Services;
using Timer = System.Timers.Timer;
using AuditLogActionType = XeniaBot.Data.Moderation.Models.AuditLogCheckRecord.AuditLogActionType;

namespace XeniaBot.Moderation.Services;

public partial class ModerationService
{
    private Timer? _kickEventTimer = new Timer();
    protected void InitKickEvent()
    {
        if (_kickEventTimer != null)
        {
            try
            { _kickEventTimer.Dispose(); }
            catch
            { }
        }
        _kickEventTimer = new Timer(TimeSpan.FromSeconds(30));
        _kickEventTimer.AutoReset = true;
        _kickEventTimer.Enabled = true;
        _kickEventTimer.Elapsed += KickEventTimer_Handle;
        if (CoreContext.Instance!.Details.Platform == XeniaPlatform.Bot)
        {
            _kickEventTimer.Start();
        }
    }

    public event MemberKickedDelegate? DiscordUserKicked;
    private void KickEventTimer_Handle(object? sender, ElapsedEventArgs args)
    {
        Log.Debug("Running task.");
        var taskList = new List<Task>();
        foreach (var item in _discordClient.Guilds)
        {
            if (!item.CurrentUser.GuildPermissions.ViewAuditLog)
            {
                Log.Warn($"Missing AuditLog permission in guild {item.Name} ({item.Id})");
                continue;
            }
            var guildId = item.Id;
            
            taskList.Add(new Task(delegate
            {
                var record = _auditCheckRepo.Get(guildId, AuditLogActionType.Kick, CoreContext.InstanceId.ToString()).Result
                             ?? new AuditLogCheckRecord()
                             {
                                 ActionType = AuditLogActionType.Kick,
                                 GuildId = guildId.ToString(),
                                 LastId = null
                             };
                var lastAuditId = record.LastId;
                
                var guild = _discordClient.GetGuild(guildId)!;
                var audit = guild
                    .GetAuditLogsAsync(100000, actionType: ActionType.Kick, afterId: lastAuditId)
                    .FlattenAsync()
                    .Result
                    .OrderByDescending(v => v.CreatedAt)
                    ?.ToList()
                    ?? new List<RestAuditLogEntry>();

                record.LastId = audit.FirstOrDefault()?.Id;
                if (record.LastId != null)
                {
                    record.ResetId();
                    record.Timestamp = audit.First()!.CreatedAt.ToUnixTimeSeconds();
                    _auditCheckRepo.Add(record).Wait();
                }

                foreach (var auditItem in audit!)
                {
                    if (!(auditItem.Data is KickAuditLogData auditData))
                        continue;

                    AddRecordKick(
                        guild, auditData.Target.Id, auditItem.User.Id, auditItem.Reason,
                        auditItem.CreatedAt.ToUnixTimeSeconds()).Wait();
                    DiscordUserKicked?.Invoke(
                        guild, 
                        auditData.Target.Id, 
                        auditItem.User.Id,
                        auditItem.Reason,
                        auditItem.CreatedAt);
                }
            }));
        }

        XeniaHelper.TaskWhenAll(taskList).Wait();
    }
}