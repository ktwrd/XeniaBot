using Discord;

namespace XeniaDiscord.Common.Services;

public class DiscordSnapshotRoleUpdateInfo
{
    public required IReadOnlyCollection<GuildPermission> Added { get; init; }
    public required IReadOnlyCollection<GuildPermission> Removed { get; init; }
}
