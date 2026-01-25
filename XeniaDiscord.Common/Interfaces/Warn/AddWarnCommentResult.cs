using XeniaDiscord.Data.Models.Warn;

namespace XeniaDiscord.Common;

public class AddWarnCommentResult
{
    public AddWarnCommentResultKind Kind { get; set; }
    public bool? IsGuildConfigured { get; set; }
    public GuildWarnCommentModel? Model { get; set; }
    public GuildWarnModel? WarnModel { get; set; }
}

public enum AddWarnCommentResultKind
{
    /// <summary>
    /// Successfully created comment.
    /// </summary>
    Success,

    /// <summary>
    /// Warn record couldn't be found in the database.
    /// </summary>
    WarnRecordNotFound,

    /// <summary>
    /// When the comment creator doesn't have permission to add a comment.
    /// User must have the <c>ModerateMembers</c> permission.
    /// </summary>
    MissingPermissions,

    /// <summary>
    /// Cannot check for user permissions, since the bot isn't in the guild anymore.
    /// Only is returned when the creator of the comment, isn't the creator of the warn (or a bot owner).
    /// </summary>
    BotNotInGuildAnymore,

    /// <summary>
    /// Comment content is null or empty.
    /// </summary>
    ContentRequired,

    /// <summary>
    /// Returned when the content provided is greater than <see cref="XeniaDiscord.Data.DatabaseHelper.MaxStringSize"/>
    /// </summary>
    ContentTooLong,


}
