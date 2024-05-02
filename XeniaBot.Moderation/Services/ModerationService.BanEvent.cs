using System.Timers;
using Discord;
using Discord.Rest;
using XeniaBot.Data.Moderation.Models;
using XeniaBot.Moderation.Helpers;
using XeniaBot.Shared;
using XeniaBot.Shared.Helpers;
using XeniaBot.Shared.Services;

namespace XeniaBot.Moderation.Services;
using Timer = System.Timers.Timer;
using AuditLogActionType = XeniaBot.Data.Moderation.Models.AuditLogCheckRecord.AuditLogActionType;

public partial class ModerationService
{
    private Timer? _banEventTimer = new Timer();

    protected void InitBanEvent()
    {
        if (_banEventTimer != null)
        {
            try
            {
                _banEventTimer.Dispose();
            }
            catch
            { }
        }

        _banEventTimer = new Timer(TimeSpan.FromSeconds(30));
        _banEventTimer.AutoReset = true;
        _banEventTimer.Enabled = true;
        _banEventTimer.Elapsed += BanEventTimer_Handle;
        if (CoreContext.Instance!.Details.Platform == XeniaPlatform.Bot)
        {
            // delay so the discord api isn't hammered
            Task.Delay(2000).Wait();
            _banEventTimer.Start();
        }
    }

    public event MemberBannedDelegate? DiscordMemberBanned;

    private void BanEventTimer_Handle(object? sender, ElapsedEventArgs args)
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
                var record =
                    _auditCheckRepo.Get(guildId, AuditLogActionType.Ban, CoreContext.InstanceId.ToString()).Result ??
                    new AuditLogCheckRecord()
                    {
                        ActionType = AuditLogActionType.Ban,
                        GuildId = guildId.ToString(),
                        LastId = null
                    };
                var lastAuditId = record.LastId;

                var guild = _discordClient.GetGuild(guildId)!;
                var audit = guild
                    .GetAuditLogsAsync(100000, actionType: ActionType.Ban, afterId: lastAuditId)
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
                    if (!(auditItem.Data is BanAuditLogData auditData))
                        continue;

                    AddRecordBan(
                        guild, auditData.Target.Id, auditItem.User.Id, auditItem.Reason,
                        auditItem.CreatedAt.ToUnixTimeSeconds(), false).Wait();
                    DiscordMemberBanned?.Invoke(
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