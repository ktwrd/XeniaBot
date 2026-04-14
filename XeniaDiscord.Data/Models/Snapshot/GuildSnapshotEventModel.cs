using System.ComponentModel.DataAnnotations;

namespace XeniaDiscord.Data.Models.Snapshot;

/// <summary>
/// Used to define what event caused a Guild Snapshot.
/// </summary>
public class GuildSnapshotEventModel
{
    public const string TableName = "SnapshotEvent_Guild";

    public GuildSnapshotEventModel()
    {
        Id = Guid.NewGuid();
        Timestamp = DateTime.UtcNow;
        GuildId = "0";
        Source = DiscordSnapshotSource.Unknown;
        BeforeId = null;
        CurrentId = Guid.Empty;
        
        Current = null!;
    }

    /// <summary>
    /// Record Id (primary key)
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// UTC Time of when this record was created
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Guild Id (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string GuildId { get; set; }

    /// <summary>
    /// Event source
    /// </summary>
    public DiscordSnapshotSource Source { get; set; }

    /// <summary>
    /// (optional) Foreign Key to <see cref="GuildSnapshotModel.RecordId"/>
    /// </summary>
    public Guid? BeforeId { get; set; }

    /// <summary>
    /// Foreign Key to <see cref="GuildSnapshotModel.RecordId"/>
    /// </summary>
    public Guid CurrentId { get; set; }

    /// <summary>
    /// Property Accessor
    /// </summary>
    public GuildSnapshotModel? Before { get; set; }

    /// <summary>
    /// Property Accessor
    /// </summary>
    public GuildSnapshotModel Current { get; set; }
}
