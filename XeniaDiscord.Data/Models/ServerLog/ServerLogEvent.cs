namespace XeniaDiscord.Data.Models.ServerLog;                   

public enum ServerLogEvent
{
    Fallback,
    MemberJoin,
    MemberLeave,
    MemberBan,
    MemberKick,

    MessageEdit,
    MessageDelete,

    ChannelDelete,
    ChannelEdit,
    ChannelCreate,

    MemberVoiceChange,
    MemberRoleAdded,
    MemberRoleRemoved,
    MemberRoleUpdated,
}