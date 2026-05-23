namespace XeniaDiscord.Data.Models.Snapshot;

public enum DiscordSnapshotSource
{
    Unknown = 0,

    MemberJoined,
    MemberUpdated,
    UserUpdated,
    UserLeft,
    UserBanned,
    UserUnbanned,

    RoleCreated,
    RoleUpdated,
    RoleDeleted,

    JoinedGuild,
    LeftGuild,
    GuildUpdated,
}
