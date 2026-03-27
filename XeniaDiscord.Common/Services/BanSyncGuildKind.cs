using System.ComponentModel;

namespace XeniaDiscord.Common.Services;

public enum BanSyncGuildKind
{
    [Description("Your server is too young. It must be at least 3 months old.")]
    TooYoung,

    [Description("Your server doesn't have enough members.")]
    NotEnoughMembers,

    [Description("Your server is blacklisted from the BanSync feature.")]
    Blacklisted,

    [Description("Missing permission \"Ban Members\"")]
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
