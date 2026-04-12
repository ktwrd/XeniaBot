using Discord;
using System.ComponentModel;

namespace XeniaBot.WebPanel.Models.Server;

public class PartialHomeViewModel
{
    public ulong GuildId { get; set; }

    public bool BanSyncEnabled { get; set; }
    public int BanSyncRecordCount { get; set; }

    public int ServerLogEnabled { get; set; }
    public int ServerLogChannelCount { get; set; }

    public GuildPermissionWarningDto[] GuildPermissionWarnings { get; set; } = [];
}

public class GuildPermissionWarningDto
{
    public ulong GuildId { get; set; }
    public ulong? ChannelId { get; set; }
    public GuildPermission[] MissingPermissions { get; set; } = [];
    public PermissionWarningSourceSystem SourceSystem { get; set; }
}
public enum PermissionWarningSourceSystem
{
    [Description("Global - Permission is required for Xenia to function in any capacity.")]
    Global,

    ServerLog,
    BanSync,
    RolePreserve
}
