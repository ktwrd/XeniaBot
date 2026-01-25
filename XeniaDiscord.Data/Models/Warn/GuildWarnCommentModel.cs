using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace XeniaDiscord.Data.Models.Warn;

public class GuildWarnCommentModel
{
    public const string TableName = "GuildWarnComment";

    public GuildWarnCommentModel()
    {
        Id = Guid.NewGuid();
        WarnId = Guid.Empty;
        CreatedAt = DateTime.UtcNow;
        Content = "";
        CreatedByUserId = "0";
    }

    [Required]
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign Key to <see cref="GuildWarnModel.Id"/>
    /// </summary>
    [Required]
    [MaxLength(DbGlobals.MaxLength.ULong)]
    public Guid WarnId { get; set; }

    /// <summary>
    /// When this comment was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    [Required]
    [MaxLength(DbGlobals.MaxStringSize)]
    public string Content { get; set; }

    /// <summary>
    /// Discord User Snowflake for the user that created this comment.
    /// </summary>
    [MaxLength(DbGlobals.MaxLength.ULong)]
    public string CreatedByUserId { get; set; }

    [DefaultValue(false)]
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Time when this comment was deleted.
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Discord User Snowflake for the user that deleted this comment.
    /// </summary>
    [MaxLength(DbGlobals.MaxLength.ULong)]
    public string? DeletedByUserId { get; set; }

    /// <summary>
    /// Reason why this comment was deleted.
    /// </summary>
    [MaxLength(DbGlobals.MaxLength.ULong)]
    public string? DeleteReason { get; set; }

    public ulong? GetCreatedByUserId()
        => CreatedByUserId.ParseRequiredULong(nameof(CreatedByUserId), false);
    public ulong? GetDeletedByUserId()
        => CreatedByUserId.ParseULong(false);
}
