using XeniaDiscord.Data.Models.DiscordSnapshot;

namespace XeniaDiscord.Data.Models.Ticket;

public class GuildTicketTranscriptMessageSnapshotModel
{
    public const string TableName = "GuildTicketTranscriptMessageSnapshot";

    public GuildTicketTranscriptMessageSnapshotModel()
    {
        Id = Guid.NewGuid();
        TicketTranscriptId = Guid.Empty;
    }

    public Guid Id { get; set; }
    /// <summary>
    /// Foreign Key to <see cref="GuildTicketTranscriptModel.Id"/>
    /// </summary>
    public Guid TicketTranscriptId { get; set; }
    /// <summary>
    /// Foreign Key to <see cref="DiscordSnapshotMessageModel.Id"/>
    /// </summary>
    public Guid MessageSnapshotId { get; set; }
    // Property Accessor
    public DiscordSnapshotMessageModel MessageSnapshot { get; set; } = new();
}
