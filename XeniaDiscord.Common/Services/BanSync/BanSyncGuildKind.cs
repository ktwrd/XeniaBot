using System.ComponentModel;

namespace XeniaDiscord.Common.Services.BanSync;

public enum BanSyncGuildKind
{
    [Description("Your server is too young. It must be at least 3 months old.")]
    TooYoung,

    [Description("Your server doesn't have enough members.")]
    NotEnoughMembers,

    [Description("Your server is blacklisted from the BanSync feature.")]
    Blacklisted,

    [Description("Xenia is missing the \"Ban Members\" permission.\n"
                 + "**This is required** to view who's been banned in your server."
                 + " ([Source](https://docs.discord.com/developers/resources/guild#get-guild-bans))")]
    MissingBanMembersPermission,

    [Description("Log Channel doesn't exist in this guild.")]
    LogChannelMissing,

    [Description("Cannot access Log Channel")]
    LogChannelCannotAccess,

    [Description("Missing permission \"Send Messages\" in Log Channel.")]
    LogChannelCannotSendMessages,

    [Description("Missing permission \"Embed Links\" in Log Channel.")]
    LogChannelCannotSendEmbeds,

    [Description("Failed to check Guild eligibility.")]
    InternalError,

    [Description("Configuration is valid!")]
    Valid,
}
