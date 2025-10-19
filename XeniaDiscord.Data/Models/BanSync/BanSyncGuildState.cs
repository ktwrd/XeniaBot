using System.ComponentModel;

namespace XeniaDiscord.Data.Models.BanSync;

public enum BanSyncGuildState : byte
{
    [Description("Unknown")]
    Unknown = 0,
    [Description("BanSync Access Request Pending")]
    PendingRequest,
    [Description("BanSync Access Request Denied")]
    RequestDenied,
    [Description("Guild blacklisted for BanSync")]
    Blacklisted,
    [Description("Bansync Access Approved")]
    Active
}
