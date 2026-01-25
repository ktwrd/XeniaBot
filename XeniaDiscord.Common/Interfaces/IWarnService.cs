using Discord;
using XeniaDiscord.Data.Models.Warn;

namespace XeniaDiscord.Common.Interfaces;

public interface IWarnService
{
    /// <summary>
    /// Warn a user.
    /// </summary>
    /// <param name="guild">Guild to create the warn in.</param>
    /// <param name="targetUser">User to warn</param>
    /// <param name="reason">Reason why this user is being warned</param>
    /// <param name="createdByUser">User that created the warning</param>
    public Task<CreateWarnResult> CreateAsync(IGuild guild, IUser targetUser, string reason, IUser createdByUser);

    public Task<string?> GetDashboardUrl(GuildWarnModel? model);

    #region Comments
    public Task<AddWarnCommentResult> AddCommentAsync(
        Guid warnId,
        string content,
        IUser createdByUser);

    public Task<AddWarnCommentResult> AddCommentAsync(
        GuildWarnModel? warnRecord,
        GuildWarnConfigModel? guildConfig,
        string content,
        IUser createdByUser);

    /// <inheritdoc cref="DeleteCommentAsync(GuildWarnModel?, GuildWarnCommentModel?, IUser, string?)"/>
    /// <param name="commentId"><see cref="GuildWarnCommentModel.Id"/> of the comment to search by</param>
    /// <param name="deletedByUser">
    /// <inheritdoc cref="DeleteCommentAsync(GuildWarnModel?, GuildWarnCommentModel?, IUser, string?)" path="/param[@name='deletedByUser']"/>
    /// </param>
    /// <param name="reason">
    /// <inheritdoc cref="DeleteCommentAsync(GuildWarnModel?, GuildWarnCommentModel?, IUser, string?)" path="/param[@name='reason']"/>
    /// </param>
    public Task<DeleteWarnCommentResult> DeleteCommentAsync(
        Guid commentId,
        IUser deletedByUser,
        string? reason);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="warnRecord">Warn Record that the comment belongs to.</param>
    /// <param name="commentModel">Comment Model to mark as deleted.</param>
    /// <param name="deletedByUser">Who is deleting the comment.</param>
    /// <param name="reason">Reason why the comment was deleted (optional)</param>
    /// <returns></returns>
    /// <remarks>
    /// If you would like to delete a comment, you must be the person who created the comment,
    /// have the "Administrator" permission, or be a superuser defined in the config file.
    /// </remarks>
    public Task<DeleteWarnCommentResult> DeleteCommentAsync(
        GuildWarnModel? warnRecord,
        GuildWarnCommentModel? commentModel,
        IUser deletedByUser,
        string? reason);
    #endregion
}
