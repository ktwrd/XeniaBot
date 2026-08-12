using kate.shared.Extensions;
using XeniaBot.Shared;
using XeniaDiscord.Data.Models.BanSync;

namespace XeniaDiscord.Common.Services.BanSync;

public class RequestBanSyncFeatureResult(ConfigData configData, BanSyncGuildKind kind, BanSyncGuildModel model)
{
    public bool Success => GuildKind == BanSyncGuildKind.Valid;
    public BanSyncGuildKind GuildKind { get; } = kind;
    public BanSyncGuildModel GuildModel { get; } = model;

    private const string MsgPendingRequest =
        "Unable to request for BanSync again, since you're already waiting for your server to be reviewed.";
    
    private const string MsgEmbedNotEnoughMembers =
        "Your server doesn't have enough members.\n" +
        "It needs at least `{0}`";
    private const string MsgWebNotEnoughMembers =
        "Unable to request for BanSync, Your server doesn't have enough members.\n" +
        "It needs at least `{0}` to request the BanSync feature.";
    
    private const string MsgLogChannelCannotSendMessages =
        "Xenia is missing the `Send Messages` permission in the configured log channel{0}";
    private const string MsgWebLogChannelCannotSendMessages =
        "Unable to request for BanSync, since " +
        MsgLogChannelCannotSendMessages;

    private const string MsgLogChannelCannotSendEmbeds =
        "Xenia is missing the `Embed Links` permission in the configured log channel{0}";
    private const string MsgWebLogChannelCannotSendEmbeds =
        "Unabel to request for BanSync feature, since " +
        MsgLogChannelCannotSendEmbeds;

    private const string MsgMissingBanMembersPermission =
        "Xenia is missing the \"Ban Members\" permission.\n" +
        "**This is required** for Xenia to view who's been banned in your server.";
    private const string MsgEmbedMissingBanMembersPermission =
        MsgMissingBanMembersPermission + "\n" +
        "-# [Source](https://docs.discord.com/developers/resources/guild#get-guild-bans)";

    private const string MsgLogChannelMissing =
        "Log channel has not been configured, please do so with the command: `/bansync setchannel`";
    private const string MsgWebLogChannelMissing =
        "Unable to request for BanSync feature since you haven't configured a log channel.";
    
    private const string MsgLogChannelCannotAccess =
        "Xenia is unable to access the log channel that you've configured.\n" +
        "Please make sure it still exists, and that Xenia has the correct permissions. " +
        "([see guide](https://xenia.kate.pet/guide/required_permissions#content-bansync))";
    private const string MsgWebLogChannelCannotAccess =
        "Unable to request for BanSync since " +
        MsgLogChannelCannotAccess;

    private const string MsgTooYoung =
        "Your server is too young, it must be at least {0}";
    private const string MsgWebTooYoung =
        "Unable to request for BanSync feature since your server is too young. it must be at least {0}";

    private const string MsgBlacklisted = "Your server is blacklist from the BanSync feature.";
    private const string MsgWebBlacklisted = "Unable to request for BanSync feature, " + MsgBlacklisted;
    
    private const string MoreInfoSuffix = "For more information, please [join our support server]({0}).";

    public string FormatMessage(FormatMessageKind formatKind)
    {
        var logChannelId = GuildModel.GetLogChannelId();
        var embedLogChannelSuffix = logChannelId.HasValue ? $": <#{logChannelId}>" : ".";
        switch (GuildKind)
        {
            case BanSyncGuildKind.PendingRequest:
                return MsgPendingRequest;
            case BanSyncGuildKind.TooYoung:
                return string.Format(
                    formatKind switch
                    {
                        FormatMessageKind.MessageEmbed => MsgTooYoung,
                        FormatMessageKind.Dashboard => MsgWebTooYoung
                    },
                    BanSyncService.MinimumServerAgeLabel);
            case BanSyncGuildKind.NotEnoughMembers:
                return string.Format(
                    formatKind switch
                    {
                        FormatMessageKind.MessageEmbed => MsgEmbedNotEnoughMembers,
                        FormatMessageKind.Dashboard => MsgWebNotEnoughMembers
                    },
                    BanSyncService.MinimumMemberLimit);
            case BanSyncGuildKind.Blacklisted:
                {
                    var r = formatKind switch
                    {
                        FormatMessageKind.MessageEmbed => MsgBlacklisted,
                        FormatMessageKind.Dashboard => MsgWebBlacklisted
                    };
                    if (!string.IsNullOrWhiteSpace(configData.SupportServerUrl))
                    {
                        r += "\n" + string.Format(MoreInfoSuffix, configData.SupportServerUrl);
                    }

                    return r;
                }
            case BanSyncGuildKind.MissingBanMembersPermission:
                return formatKind switch
                {
                    FormatMessageKind.MessageEmbed => MsgEmbedMissingBanMembersPermission,
                    FormatMessageKind.Dashboard => MsgMissingBanMembersPermission
                };
            case BanSyncGuildKind.LogChannelMissing:
                return formatKind switch
                {
                    FormatMessageKind.MessageEmbed => MsgLogChannelMissing,
                    FormatMessageKind.Dashboard => MsgWebLogChannelMissing
                };
                break;
            case BanSyncGuildKind.LogChannelCannotAccess:
                return formatKind switch
                {
                    FormatMessageKind.MessageEmbed => MsgLogChannelCannotAccess,
                    FormatMessageKind.Dashboard => MsgWebLogChannelCannotAccess
                };
            case BanSyncGuildKind.LogChannelCannotSendMessages:
                return formatKind switch
                {
                    FormatMessageKind.MessageEmbed => string.Format(MsgLogChannelCannotSendMessages, embedLogChannelSuffix),
                    FormatMessageKind.Dashboard => string.Format(MsgWebLogChannelCannotSendMessages, ".")
                };
            case BanSyncGuildKind.LogChannelCannotSendEmbeds:
                return formatKind switch
                {
                    FormatMessageKind.MessageEmbed => string.Format(MsgLogChannelCannotSendEmbeds, embedLogChannelSuffix),
                    FormatMessageKind.Dashboard => string.Format(MsgWebLogChannelCannotSendEmbeds, ".")
                };
            case BanSyncGuildKind.InternalError:
                return "Failed to request for BanSync feature: Internal error.";
            case BanSyncGuildKind.Valid:
                return "Congratulations! Your server is eligible for the BanSync Feature!";

        }

        return GuildKind.ToDescriptionString(GuildKind.ToString());
    }
}

public enum FormatMessageKind
{
    /// <summary>
    /// Format message for use in a Discord Embed.
    /// </summary>
    MessageEmbed,
    /// <summary>
    /// Format message for use in the Dashboard. (sanitized markdown)
    /// </summary>
    Dashboard
}