using XeniaDiscord.Data.Models.Warn;

namespace XeniaDiscord.Common;

public class CreateWarnResult
{
    public CreateWarnResultKind Kind { get; set; }
    public bool IsGuildConfigured { get; set; }
    public GuildWarnModel? Model { get; set; }
}

public enum CreateWarnResultKind
{
    Success,
    MissingPermissions,
    MissingReason,
    ReasonTooLong
}
