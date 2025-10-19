using System;
using System.Collections.Generic;
using System.ComponentModel;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using XeniaBot.Shared.Models;

namespace XeniaBot.Data.Models;

public class GuildWarnCommentModel : BaseModelGuid
{
    public const string CollectionName = "guildWarnComments";
    public GuildWarnCommentModel()
        : base()
    {
        WarnId = Guid.Empty.ToString();
        CreatedByUserId = "0";
        CreatedByFallbackUsername = "unknown";
        CreatedByFallbackDisplayName = "Unknown User";
        CreatedAt = new BsonTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        AttachmentIds = [];
    }
    /// <summary>
    /// Foreign Key to <see cref="GuildWarnItemModel.WarnId"/>
    /// </summary>
    public string WarnId { get; set; }
    
    public string CreatedByUserId { get; set; }
    public string CreatedByFallbackUsername { get; set; }
    public string CreatedByFallbackDisplayName { get; set; }

    public string Content { get; set; } = "";
    
    [BsonIgnoreIfDefault]
    [DefaultValue(false)]
    public bool IsDeleted { get; set; }
    
    [BsonIgnoreIfNull]
    public BsonTimestamp? DeletedAt { get; set; }
    
    [BsonIgnoreIfNull]
    public string? DeletedByUserId { get; set; }
    [BsonIgnoreIfNull]
    public string? DeletedByFallbackUsername { get; set; }
    [BsonIgnoreIfNull]
    public string? DeletedByFallbackDisplayName { get; set; }
    
    public BsonTimestamp CreatedAt { get; set; }

    /// <summary>
    /// List of foreign keys to <see cref="AttachmentModel"/>
    /// </summary>
    public List<string> AttachmentIds { get; set; }
}