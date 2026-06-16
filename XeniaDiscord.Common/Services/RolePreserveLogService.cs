using System.Text;
using CSharpFunctionalExtensions;
using Discord;
using Discord.WebSocket;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using XeniaBot.Shared;
using XeniaBot.Shared.Helpers;
using XeniaBot.Shared.Services;
using XeniaDiscord.Data.Models.RolePreserve;
using XeniaDiscord.Data.Models.ServerLog;
using XeniaDiscord.Data.Repositories;

namespace XeniaDiscord.Common.Services;

public class RolePreserveLogService
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly ErrorReportService _err;
    private readonly ServerLogRepository _serverLogRepo;
    private readonly ConfigData _config;
    
    public RolePreserveLogService(IServiceProvider services)
    {
        _err = services.GetRequiredService<ErrorReportService>();
        _serverLogRepo = services.GetRequiredService<ServerLogRepository>();
        _config = services.GetRequiredService<ConfigData>();
    }

    public async Task SendAuditNotification(
        SocketGuildUser user,
        RolePreserveAuditModel auditModel)
    {
        if (auditModel.AppliedRoles.Any(e => e.IsActionFailure()))
        {
            await SendFailureNotification(user, auditModel);
        }
    }
    
    public async Task SendFailureNotification(
        SocketGuildUser user,
        RolePreserveAuditModel auditModel)
    {
        // don't run method if there are no failures
        if (auditModel.AppliedRoles.All(e => e.IsActionSuccess() || e.IsActionSkip()))
            return;

        var targetLogChannels = await GetLogChannelsFor(RolePreserveLogKind.Failure, user);
        if (targetLogChannels.Count < 1) return;

        var successCountValue = auditModel.AppliedRoles.Count(e => e.IsActionSuccess());
        var skipCountValue = auditModel.AppliedRoles.Count(e => e.IsActionSkip());
        var failCountValue = auditModel.AppliedRoles.Count(e => e.IsActionFailure());
        if (skipCountValue < 1 && failCountValue < 1) return;

        var embed = new EmbedBuilder()
            .WithDescription(FormatDescriptionText(auditModel))
            .WithTitle("Role Preserve - Failure - " + user.Username)
            .WithFooter($"User Id: {user.Id}")
            .WithColor(new Color(255, 255, 255))
            .WithCurrentTimestamp();
        if (_config.HasDashboard)
        {
            embed.WithUrl($"{_config.DashboardUrl}/RolePreserve/Audit/Details?Id={auditModel.Id}");
        }

        NotificationGetDescription(embed, auditModel);
        var attachments = GenerateAttachments(embed, RolePreserveLogKind.Failure, auditModel);
        
        _log.Info($"Audit ID={auditModel.Id}, Guild ID={user.Guild.Id}, User ID={user.Id}, Username={user.Username} (log channels: {targetLogChannels.Count}, success: {successCountValue}, skip: {skipCountValue}, fail: {failCountValue})");
        
        foreach (var serverLogChannel in targetLogChannels)
        {
            await SendNotificationToChannel(user, embed, attachments, serverLogChannel);
        }
    }

    private async Task SendNotificationToChannel(
        SocketGuildUser user,
        EmbedBuilder embed,
        List<FileAttachment> attachments,
        ServerLogChannelModel serverLogChannel)
    {
        SocketTextChannel? textChannel;
        try
        {
            textChannel = ExceptionHelper.RetryOnTimedOut(() => user.Guild.GetTextChannel(serverLogChannel.GetChannelId()))
                          ?? throw new InvalidOperationException($"Channel {serverLogChannel.ChannelId} does not exist (GetTextChannel returned null)");
        }
        catch (Exception ex)
        {
            _log.Warn(ex, $"Could not get channel {serverLogChannel.ChannelId} in Guild \"{user.Guild}\" ({user.Guild.Id}) from ServerLogChannel with Id={serverLogChannel.Id}");
            return;
        }
        try
        {
            if (attachments.Count > 0)
            {
                await ExceptionHelper.RetryOnTimedOut(async () => await textChannel.SendFilesAsync(attachments, embed: embed.Build()));
            }
            else
            {
                await ExceptionHelper.RetryOnTimedOut(async () => await textChannel.SendMessageAsync(embed: embed.Build()));
            }
        }
        catch (Exception ex)
        {
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes($"Failed to send message in channel \"{textChannel.Name}\" ({textChannel.Id}) in Guild \"{user.Guild.Name}\" ({user.Guild.Id}) for user \"{user.Username}#{user.Discriminator}\" ({user.Id})")
                .WithUser(user)
                .WithGuild(user.Guild)
                .WithChannel(textChannel)
                .AddSerializedAttachment("serverLogChannel.json", serverLogChannel));
        }
    }

    private static string FormatDescriptionText(RolePreserveAuditModel auditModel)
    {
        const string defaultResult = "No actions applied.";
        var successCountValue = auditModel.AppliedRoles.Count(e => e.IsActionSuccess());
        var skipCountValue = auditModel.AppliedRoles.Count(e => e.IsActionSkip());
        var failCountValue = auditModel.AppliedRoles.Count(e => e.IsActionFailure());
        if (skipCountValue < 1 && failCountValue < 1) return defaultResult;
        
        var descriptionText = new List<string>(3);
        if (successCountValue > 0)
            descriptionText.Add("successfully added " + "role".ToQuantity(successCountValue, "N0"));
        if (skipCountValue > 0)
            descriptionText.Add("skipped " + "role".ToQuantity(skipCountValue, "N0"));
        if (failCountValue > 0)
            descriptionText.Add("failed to add " + "role".ToQuantity(failCountValue, "N0"));
        
        var count = descriptionText.Count;
        
        if (count < 1) return defaultResult;
        
        // make sure first char in first string is uppercase.
        var fx = descriptionText[0].ToCharArray();
        fx[0] = char.ToUpper(fx[0]);
        descriptionText[0] = new string(fx);

        switch (count)
        {
            case 1:
                return descriptionText[0] + ".";
            case 2:
                return descriptionText[0] + " and " + descriptionText[1] + ".";
            default:
            {
                var others = string.Join(", ", descriptionText.Take(count - 1));
                return others + ", and " + descriptionText.Last() + ".";
            }
        }
    }
    
    private static void NotificationGetDescription(
        EmbedBuilder embed,
        RolePreserveAuditModel auditModel)
    {
        var successCountValue = auditModel.AppliedRoles.Count(e => e.IsActionSuccess());
        var skipCountValue = auditModel.AppliedRoles.Count(e => e.IsActionSkip());
        var failCountValue = auditModel.AppliedRoles.Count(e => e.IsActionFailure());

        var sb = new StringBuilder();
        if (successCountValue > 0 && failCountValue == 0)
        {
            sb.Append("- ");
            sb.Append(Emotes.Tada);
            sb.Append("Successfully granted ");
            sb.Append("role".ToQuantity(successCountValue));
            sb.Append(" to user.");
        }
        else if (successCountValue > 0 && failCountValue > 0)
        {
            sb.Append("- ");
            sb.Append(Emotes.Warning);
            sb.Append(" Successfully granted user");
            sb.Append("role".ToQuantity(successCountValue));
            sb.Append(", but failed to grant them");
            sb.Append("role".ToQuantity(failCountValue));
            sb.Append('.');
        }
        else if (failCountValue > 0)
        {
            sb.Append("- ");
            sb.Append(Emotes.Warning);
            sb.Append(" Failed to give user *any* roles. ");
            sb.AppendFormat("({0})", "failure".ToQuantity(failCountValue));
        }

        if (sb.Length > 0 && skipCountValue > 0)
        {
            sb.Append('\n');
            sb.Append("-# Skipped ");
            sb.Append("role".ToQuantity(skipCountValue));
        }

        if (sb.Length > 0) embed.WithDescription(sb.ToString());
    }

    #region Send Notification - Generate Success/Failure Info
    private List<FileAttachment> GenerateAttachments(
        EmbedBuilder embed,
        RolePreserveLogKind kind,
        RolePreserveAuditModel auditModel)
    {
        return kind switch
        {
            RolePreserveLogKind.Failure => GenerateAttachmentsForFail(embed, auditModel),
            _ => throw new NotImplementedException($"For {nameof(RolePreserveLogKind)}={kind}")
        };
    }

    private List<FileAttachment> GenerateAttachmentsForFail(
        EmbedBuilder embed,
        RolePreserveAuditModel auditModel)
    {
        const string failFilename = "roles-failure.txt";
        var attachments = new List<FileAttachment>();
        var content = GenerateFailureEmbedContent(auditModel);
        if (content.HasNoValue)
        {
            attachments.Add(new FileAttachment(
                GenerateFailureAttachmentContent(auditModel).ToMemoryStream(Encoding.UTF8),
                failFilename,
                "List of all the roles that Xenia failed to give to a user."));
        }

        var text = $"{Emotes.Warning} Unknown error, no attachments generated and no content generated!";
        if (content.HasValue)
        {
            embed.AddField("Failed Roles", content.Value);
        }
        else if (attachments.Count > 0)
        {
            text = $"{Emotes.Warning} Too many roles for an embed! It has been attached as {failFilename}";
            GetAuditEventUrl(auditModel)
                .Tap(url =>
                {
                    text += $"\n-# [View audit log entry]({url}).";
                });
        }

        embed.AddField("Failed Roles", text);
        return attachments;
    }

    private static string GenerateFailureAttachmentContent(
        RolePreserveAuditModel auditModel)
    {
        var sb = new StringBuilder();
        foreach (var item in auditModel.AppliedRoles.Where(e => e.IsActionFailure()))
        {
            sb.Append(item.RoleId);
            if (!string.IsNullOrEmpty(item.Role?.Name))
            {
                sb.Append(" - ");
                sb.Append(item.Role.Name);
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }

    /// <returns>
    /// <see cref="Maybe.None"/> if the roles should be attached instead of an embed field.
    /// </returns>
    private static Maybe<string> GenerateFailureEmbedContent(
        RolePreserveAuditModel auditModel)
    {
        /* determined with the following code:
        const int max = 1024;
        int lineSize = string.Format("- <@&{0}>\n", ulong.MaxValue).Length; // expected to be 26
        int iterCount = Convert.ToInt32(Math.Floor(max / (float)(lineSize)));
         */
        const int maxCountSafe = 37;
        var items = auditModel.AppliedRoles
            .Where(e => e.IsActionFailure())
            .ToArray();
        var count = items.Length;
        if (count > maxCountSafe) return Maybe.None;

        var sb = new StringBuilder();
        for (var i = 0; i < count; i++)
        {
            var item = items.ElementAt(i);
            sb.Append("- <@&");
            sb.Append(item.RoleId);
            sb.Append('>');
            if (i < count - 1)
            {
                sb.AppendLine();
            }
        }
        // added just to be safe
        if (sb.Length >= 1024) return Maybe.None;
        return sb.ToString();
    }
    #endregion

    public Maybe<string> GetAuditEventUrl(RolePreserveAuditModel auditModel)
    {
        if (!_config.HasDashboard) return Maybe.None;
        return $"{_config.DashboardUrl}/RolePreserve/Audit/Details?Id={auditModel.Id}";
    }
    
    #region Get Log Channels
    public async Task<IReadOnlyCollection<ServerLogChannelModel>> GetLogChannelsFor(
        RolePreserveLogKind kind,
        ulong guildId)
    {
        try
        {
            return await GetLogChannelsForInternal(kind, guildId);
        }
        catch (Exception ex)
        {
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes($"Failed to get Server Log Channel models with event {ServerLogEvent.MemberJoin} or {ServerLogEvent.RolePreserve} for Guild  {guildId}"));
            return [];
        }
    }
    public async Task<IReadOnlyCollection<ServerLogChannelModel>> GetLogChannelsFor(
        RolePreserveLogKind kind,
        SocketGuildUser user)
    {
        try
        {
            return await GetLogChannelsForInternal(kind, user.Guild.Id);
        }
        catch (Exception ex)
        {
            await _err.Submit(new ErrorReportBuilder()
                .WithException(ex)
                .WithNotes($"Failed to get Server Log Channel models with event {ServerLogEvent.MemberJoin} or {ServerLogEvent.RolePreserve} for Guild \"{user.Guild.Name}\" ({user.Guild.Id})")
                .WithUser(user)
                .WithGuild(user.Guild));
            return [];
        }
    }

    private async Task<IReadOnlyCollection<ServerLogChannelModel>> GetLogChannelsForInternal(
        RolePreserveLogKind kind,
        ulong guildId)
    {
        var targets = await _serverLogRepo.GetChannelsForGuild(
            guildId,
            [ServerLogEvent.RolePreserve],
            new()
            {
                IgnoreDisabledGuilds = true
            });
        if (kind == RolePreserveLogKind.Failure && targets.Count < 1)
        {
            targets = await _serverLogRepo.GetChannelsForGuild(
                guildId,
                [ServerLogEvent.MemberJoin],
                new()
                {
                    IgnoreDisabledGuilds = true
                });
        }

        return targets;
    }
    #endregion
}

public enum RolePreserveLogKind
{
    Success,
    Failure
}