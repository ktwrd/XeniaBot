using Discord;
using System.ComponentModel.DataAnnotations;
using XeniaDiscord.Data.Models.BanSync;

namespace XeniaDiscord.Data.Models.Cache;

public class GuildMemberCacheModel
{
    public const string TableName = "Cache_GuildMember";

    public GuildMemberCacheModel()
    {
        GuildId = "0";
        UserId = "0";
        IsMember = false;
        RecordCreatedAt = DateTime.UtcNow;
        RecordUpdatedAt = RecordCreatedAt;

        Guild = null!;
        RelatedBanSyncRecords = null!;
    }
    public GuildMemberCacheModel(IGuildUser guildUser) : this()
    {
        GuildId = guildUser.GuildId.ToString();
        UserId = guildUser.Id.ToString();
    }

    /// <summary>
    /// Guild Id (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string GuildId { get; set; }

    /// <summary>
    /// User Id (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string UserId { get; set; }

    /// <summary>
    /// Is this user currently in the <see cref="Guild"/>?
    /// </summary>
    public bool IsMember { get; set; }

    /// <summary>
    /// From: <see cref="IUser.IsBot"/>
    /// </summary>
    public bool IsBot { get; set; }
    
    /// <summary>
    /// From: <see cref="IUser.IsWebhook"/>
    /// </summary>
    public bool IsWebhook { get; set; }

    /// <summary>
    /// UTC Time for when this user joined <see cref="Guild"/>
    /// </summary>
    public DateTime? JoinedAt { get; set; }
    /// <summary>
    /// UTC Time for when this user first joined <see cref="Guild"/>
    /// </summary>
    public DateTime? FirstJoinedAt { get; set; }

    /// <summary>
    /// Nickname for user in guild. <see cref="IGuildUser.Nickname"/>
    /// </summary>
    public string? Nickname { get; set; }

    /// <summary>
    /// UTC Time when this record was created.
    /// </summary>
    public DateTime RecordCreatedAt { get; set; }
    /// <summary>
    /// UTC Time when this record was updated.
    /// </summary>
    public DateTime RecordUpdatedAt { get; set; }

    #region Property Accessors
    public GuildCacheModel Guild { get; set; }

    public List<BanSyncRecordModel> RelatedBanSyncRecords { get; set; }
    #endregion
}
