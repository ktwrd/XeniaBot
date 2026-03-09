using System.ComponentModel.DataAnnotations;
using XeniaDiscord.Data.Models.BanSync;

namespace XeniaDiscord.Data.Models.Cache;

public class GuildMemberCacheModel
{
    public const string TableName = "Cache_GuildMember";

    /// <summary>
    /// Guild Id (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string GuildId { get; set; } = "0";

    /// <summary>
    /// User Id (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string UserId { get; set; } = "0";

    /// <summary>
    /// Is this user currently in the <see cref="Guild"/>?
    /// </summary>
    public bool IsMember { get; set; }

    /// <summary>
    /// UTC Time for when this user joined <see cref="Guild"/>
    /// </summary>
    public DateTime? JoinedAt { get; set; }
    /// <summary>
    /// UTC Time for when this user first joined <see cref="Guild"/>
    /// </summary>
    public DateTime? FirstJoinedAt { get; set; }

    /// <summary>
    /// UTC Time when this record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// UTC Time when this record was updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public GuildCacheModel Guild { get; set; } = null!;

    public List<BanSyncRecordModel> RelatedBanSyncRecords { get; set; } = [];
}
