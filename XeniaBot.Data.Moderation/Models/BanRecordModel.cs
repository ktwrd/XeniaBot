using MongoDB.Bson.Serialization.Attributes;

namespace XeniaBot.Data.Moderation.Models;

public class BanRecordModel
{
    public static string CollectionName => "mod_banRecord";
    [BsonElement("_id")]
    public Guid Id { get; set; }
    
    /// <summary>
    /// Guild Id this Ban Record belongs to
    /// </summary>
    public ulong GuildId { get; set; }
    
    /// <summary>
    /// Timestamp when this record was initially added to the database.
    ///
    /// Seconds since UTC Epoch
    /// </summary>
    public long Timestamp { get; set; }
    
    /// <summary>
    /// Timestamp when this ban record was created. Will use <see cref="Timestamp"/> if it was fetched from the <see cref="Discord.IGuild.GetBansAsync"/>.
    ///
    /// Seconds since UTC Epoch
    /// </summary>
    public long CreatedAt { get; set; }
    /// <summary>
    /// User that was banned.
    /// </summary>
    public ulong UserId { get; set; }
    /// <summary>
    /// User who banned <see cref="TargetUserId"/>. Will be `null` if we don't know.
    /// </summary>
    public ulong? ActionedUserId { get; set; }

    /// <summary>
    /// Ban Reason.
    /// </summary>
    public string? Reason { get; set; }

    public BanRecordModel()
    {
        Id = Guid.NewGuid();
    }
}