using MongoDB.Bson.Serialization.Attributes;

namespace XeniaBot.Data.Moderation.Models;

public class AuditLogCheckRecord
{
    public static string CollectionName => "mod_auditLogCheck";
    [BsonElement("id")]
    public Guid Id { get; set; }

    public ulong GuildId { get; set; }
    public string ActionType { get; set; }
    public long Timestamp { get; set; }
    public long InsertTimestamp { get; set; }
    public ulong? LastId { get; set; }

    public AuditLogCheckRecord()
    {
        Id = Guid.NewGuid();
    }

}