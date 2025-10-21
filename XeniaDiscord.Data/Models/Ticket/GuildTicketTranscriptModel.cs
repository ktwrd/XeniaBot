namespace XeniaDiscord.Data.Models.Ticket;

public class GuildTicketTranscriptModel
{
    public Guid Id { get; set; }
    /// <summary>
    /// Foreign Key to <see cref="GuildTicketModel.Id"/>
    /// </summary>
    public Guid TicketId { get; set; }

    // Property Accessor
    public List<GuildTicketTranscriptMessageSnapshotModel> Messages { get; set; } = [];

    public new string[] ToString()
    {
        throw new NotImplementedException();
        // old code from mongodb project
        /*var lines = new List<string>();

        foreach (var item in Messages)
        {
            lines = lines.Concat(new string[]
            {
                $"+++Message by {item.AuthorUsername}#{item.AuthorDiscriminator} ({item.AuthorId}, ID {item.Id}, channel {item.ChannelName} {item.ChannelId})+++",
                $"-Time: {item.Timestamp}",
            }).ToList();
            foreach (var att in item.AttachmentUrls)
                lines.Add($"-Attachment: {att}");
            foreach (var emb in item.EmbedJsons)
                lines.Add($"-Embed: {emb}");
            lines.Add(item.CleanContent + "\n\n\n");
        }

        return lines.ToArray();*/
    }
}
