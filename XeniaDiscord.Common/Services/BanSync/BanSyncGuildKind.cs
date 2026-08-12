using System.ComponentModel;

namespace XeniaDiscord.Common.Services.BanSync;

public enum BanSyncGuildKind
{
    [Description("Guild is already waiting for BanSync feature to be enabled!")]
    PendingRequest,

    [Description("Your server is too young. It must be at least 12 weeks old.")]
    TooYoung,

    [Description("Not enough members, needs at least 35.")]
    NotEnoughMembers,

    [Description("Your server is blacklisted from the BanSync feature.")]
    Blacklisted,

    [Description("Xenia is missing the \"Ban Members\" permission.\n"
                 + "**This is required** to see who's been banned in your server."
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
