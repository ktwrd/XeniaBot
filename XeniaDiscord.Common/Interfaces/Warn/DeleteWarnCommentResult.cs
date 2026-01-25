using XeniaDiscord.Data.Models.Warn;

namespace XeniaDiscord.Common;

public class DeleteWarnCommentResult
{
    public DeleteWarnCommentResultKind Kind { get; set; }
    public GuildWarnCommentModel? CommentModel { get; set; }
    public GuildWarnModel? WarnModel { get; set; }
}

public enum DeleteWarnCommentResultKind
{
    Success,
    WarnNotFound,
    CommentNotFound,
    MissingPermissions,
    ReasonTooLong
}
