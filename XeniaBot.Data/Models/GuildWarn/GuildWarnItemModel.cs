using System;
using kate.shared.Helpers;
using XeniaBot.Shared.Models;

namespace XeniaBot.Data.Models;

public class GuildWarnItemModel : BaseModel
{
    public static string CollectionName => "guildWarnItems";
    
    public string WarnId { get; set; }
    public ulong GuildId { get; set; }
    /// <summary>
    /// User Id for who this warn is for
    /// </summary>
    public ulong TargetUserId { get; set; }
    /// <summary>
    /// User Id for who warned <see cref="TargetUserId"/>
    /// </summary>
    public ulong ActionedUserId { get; set; }
    /// <summary>
    /// User Id for who last updated this model.
    /// </summary>
    public ulong UpdatedByUserId { get; set; }
    /// <summary>
    /// Unix Timestamp (UTC, Milliseconds)
    /// </summary>
    public long ModifiedAtTimestamp { get; set; }
    /// <summary>
    /// Unix Timestamp (UTC, Milliseconds)
    /// </summary>
    public long CreatedAtTimestamp { get; set; }
    public string Description { get; set; }
    
    /// <summary>
    /// <para>Related Message Ids. Could be the reason why they were warned.</para>
    ///
    /// <para>Foreign Key (Many to Many) to <see cref="XeniaBot.DiscordCache.Models.CacheMessageModel.Snowflake"/></para>
    /// </summary>
    public ulong[] RelatedMessageIds { get; set; }
    /// <summary>
    /// <para>Attached media. Could be screenshots of evidence</para>
    /// 
    /// <para>Foreign Key to <see cref="ArchivedAttachmentModel.Id"/></para>
    /// </summary>
    public string[] RelatedAttachmentGuids { get; set; }

    public GuildWarnItemModel()
        : base()
    {
        WarnId = Guid.NewGuid().ToString();

        RelatedMessageIds = Array.Empty<ulong>();
        RelatedAttachmentGuids = Array.Empty<string>();
    }
}