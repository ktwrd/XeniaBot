using Discord;

namespace XeniaDiscord.Common;

public interface IWarnAdminService
{
    public Task<WarnAdminSetLogChannelResponseKind> SetLogChannel(
        IGuild guild,
        IUser createdByUser,
        ITextChannel targetChannel);
}

public enum WarnAdminSetLogChannelResponseKind
{
    Success,
    MissingPermissions,
    CannotAccessChannel
}
public enum WarnAdminEnableLoggingResponseKind
{
    Success,
    MissingPermissions,
    LogChannelNotSet
}
