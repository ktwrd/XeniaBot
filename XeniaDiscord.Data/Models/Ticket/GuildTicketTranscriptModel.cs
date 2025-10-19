using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XeniaDiscord.Data.Models.Ticket;

public class GuildTicketTranscriptModel
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    // todo port cache to sql fuck me sideways
    //property accessor
    public List<GuildTicketTranscriptMessageModel> Messages { get; set; } = [];

    public new string[] ToString()
    {
        var lines = new List<string>();

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

        return lines.ToArray();
    }
}
