namespace XeniaDiscord.Data.Models.Snapshot;

public interface ISnapshot
{
    /// <summary>
    /// Unique Record Id (Primary Key)
    /// </summary>
    public Guid RecordId { get; set; }
    /// <summary>
    /// UTC Time when this record was created.
    /// </summary>
    public DateTime RecordCreatedAt { get; set; }
}