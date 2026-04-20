using CSharpFunctionalExtensions;
using Discord;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using XeniaBot.Shared.Services;
using XeniaDiscord.Data;
using XeniaDiscord.Data.Models.ServerLog;
using XeniaDiscord.Data.Models.Snapshot;

namespace XeniaDiscord.Common.Services;

public class ServerLogEventHandler
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly ServerLogService _serverLogService;
    private readonly ErrorReportService _errorService;

    public ServerLogEventHandler(IServiceProvider services)
    {
        _serverLogService = services.GetRequiredService<ServerLogService>();
        _errorService = services.GetRequiredService<ErrorReportService>();
    }

    public async Task HandleGuildRoleUpdate(
        XeniaDbContext db,
        GuildRoleSnapshotModel? before,
        GuildRoleSnapshotModel model)
    {
        var guildIdStr = model.GuildId;
        if (before == null && model.SnapshotSource == GuildRoleSnapshotSource.RoleEdit)
        {
            _log.Trace($"Event skipped. No before state for Edit source (guildId={model.GuildId}, roleId={model.RoleId}, recordId={model.Id})");
            return;
        }
        DiscordSnapshotRoleUpdateInfo? info = null;
        try
        {
            // disabled in guild, ignore
            if (!await db.ServerLogGuilds.AnyAsync(e => e.GuildId == guildIdStr && e.Enabled))
            {
                _log.Trace($"Event skipped. Server Logging disabled in Guild (guildId={model.GuildId}, roleId={model.RoleId}, recordId={model.Id})");
                return;
            }

            var permissionsAddedList = new List<GuildPermission>();
            var permissionsRemovedList = new List<GuildPermission>();

            if (model.SnapshotSource == GuildRoleSnapshotSource.RoleDelete)
            {
                permissionsRemovedList.AddRange(model.Permissions.Select(e => e.GetValue()));
            }
            else if (model.SnapshotSource == GuildRoleSnapshotSource.RoleCreate)
            {
                permissionsAddedList.AddRange(model.Permissions.Select(e => e.GetValue()));
            }
            else if (before != null)
            {
                permissionsAddedList.AddRange(model.Permissions
                    .Where(a => !before.Permissions.Any(b => b.Value == a.Value))
                    .Select(e => e.GetValue()));
                permissionsRemovedList.AddRange(before.Permissions
                    .Where(b => !model.Permissions.Any(a => a.Value == b.Value))
                    .Select(e => e.GetValue()));
            }
            else
            {
                permissionsAddedList.AddRange(model.Permissions.Select(e => e.GetValue()));
            }
            info = new()
            {
                PermissionsAdded = permissionsAddedList,
                PermissionsRemoved = permissionsRemovedList,
                SnapshotBefore = before,
                Snapshot = model,
            };
            if (!info.Any) return;

            _log.Trace($"Handling event (source={model.SnapshotSource}, guildId={model.GuildId}, roleId={model.RoleId}, recordId={model.Id})");

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var embed = new EmbedBuilder()
                .WithDescription(string.Join("\n",
                    $"<@&{model.RoleId}>",
                    "Name: `" + model.Name?.Replace("`", "'") + "`"))
                .WithFooter("ID: " + model.RoleId)
                .WithColor(Color.Blue)
                .WithCurrentTimestamp();
            var attachments = new List<FileAttachment>();

            var targetEvent = model.SnapshotSource switch
            {
                GuildRoleSnapshotSource.RoleCreate => ServerLogEvent.RoleCreate,
                GuildRoleSnapshotSource.RoleEdit => ServerLogEvent.RoleEdit,
                GuildRoleSnapshotSource.RoleDelete => ServerLogEvent.RoleDelete,
                _ => Maybe<ServerLogEvent>.None
            };

            switch (model.SnapshotSource)
            {
                case GuildRoleSnapshotSource.RoleCreate:
                    embed.WithTitle("Role Created");
                    info.WithInfo(embed, attachments);
                    info.WithPermissionsUpdated(embed, attachments);
                    break;
                case GuildRoleSnapshotSource.RoleEdit:
                    embed.WithTitle("Role Updated")
                         .WithDescription(string.Join("\n",
                        $"<@&{model.RoleId}> was updated <t:{now}:R> (name: {model.Name?.Replace("`", "'")})",
                        "Name: `" + model.Name?.Replace("`", "'") + "`"));
                    info.WithInfo(embed, attachments);
                    info.WithPermissionsUpdated(embed, attachments);
                    break;
                case GuildRoleSnapshotSource.RoleDelete:
                    embed.WithTitle("Role Deleted");
                    info.WithPermissionsUpdated(embed, attachments);
                    break;
            }
            if (targetEvent.HasValue)
            {
                await _serverLogService.EventHandle(
                    model.GetGuildId(),
                    targetEvent.Value,
                    [embed],
                    attachments);
            }
        }
        catch (Exception ex)
        {
            var msg = $"Failed to handle event for Guild {model.GuildId} and Role {model.RoleId} (SnapshotId={model.Id}, SnapshotSource={model.SnapshotSource})";
            _log.Error(ex, msg);
            await _errorService.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes(msg)
                .AddSerializedAttachment("snapshotInfo.json", info)
                .AddSerializedAttachment("snapshot.before.json", before)
                .AddSerializedAttachment("snapshot.after.json", model));
        }
    }
}
