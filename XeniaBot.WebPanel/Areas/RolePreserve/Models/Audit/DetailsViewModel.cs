using System.Collections.Generic;
using Discord;
using Discord.WebSocket;
using XeniaBot.WebPanel.Models;
using XeniaDiscord.Data.Models.RolePreserve;
using XeniaDiscord.Data.Models.Snapshot;

namespace XeniaBot.WebPanel.Areas.RolePreserve.Models.Audit;

public class DetailsViewModel
{
    public SocketGuild? Guild { get; set; }
    public GuildSnapshotModel? GuildSnapshot { get; set; }

    public IUser? User { get; set; }
    public UserSnapshotModel? UserSnapshot { get; set; }
    
    public IUser? TargetUser { get; set; }
    public UserSnapshotModel? TargetUserSnapshot { get; set; }

    public AlertComponentViewModel? Alert { get; set; }
    public ulong GuildId { get; set; }
    public required RolePreserveAuditModel Record { get; set; }

    public Dictionary<string, StrippedRole> RoleLookup { get; set; } = [];
}
