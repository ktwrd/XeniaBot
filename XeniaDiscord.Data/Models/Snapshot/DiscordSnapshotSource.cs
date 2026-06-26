namespace XeniaDiscord.Data.Models.Snapshot;

public enum DiscordSnapshotSource
{
    Unknown = 0,

    MemberJoined,
    MemberUpdated,
    UserUpdated,
    UserLeft,
    UserBanned,
    UserUnballed,

    RoleCreated,
    RoleUpdated,
    RoleDeleted,

    JoinedGuild,
    LeftGuild,
    GuildUpdated,
}
