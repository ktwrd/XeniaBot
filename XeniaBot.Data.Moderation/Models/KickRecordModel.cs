using MongoDB.Bson.Serialization.Attributes;

namespace XeniaBot.Data.Moderation.Models;

public class KickRecordModel
{
    public static string CollectionName => "mod_kickRecord";
    
    [BsonElement("_id")]
    public Guid Id { get; set; }
    
    /// <summary>
    /// What guild the user was kicked in.
    /// </summary>
    public ulong GuildId { get; set; }
    /// <summary>
    /// User Id of who was kicked.
    /// </summary>
    public ulong UserId { get; set; }
    /// <summary>
    /// Who kicked that user.
    /// </summary>
    public ulong? ActionedByUserId { get; set; }
    /// <summary>
    /// Reason why the member was kicked.
    /// </summary>
    public string? Reason { get; set; }
    
    /// <summary>
    /// When this record was created. Seconds since Unix Epoch (UTC)
    /// </summary>
    public long CreatedAt { get; set; }

    public KickRecordModel()
    {
        Id = Guid.NewGuid();
    }
}