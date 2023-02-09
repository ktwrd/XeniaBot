using Discord;
using Discord.Rest;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SkidBot.Core.Models
{
    public class TicketTranscriptMessage/* : IMessage*/
    {
        public DateTimeOffset CreatedAt { get; set; }
        public ulong Id { get; set; }
        public MessageType Type { get; set; }
        public MessageSource Source { get; set; }
        public bool IsTTS { get; set; }
        public bool IsPinned { get; set; }
        public bool IsSuppressed { get; set; }
        public bool MentionedEveryone { get; set; }
        public string Content { get; set; }
        public string CleanContent { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public DateTimeOffset? EditedTimestamp { get; set; }
        public ulong ChannelId { get; set; }
        public string ChannelName { get; set; }
        public string AuthorUsername { get; set; }
        public string AuthorDiscriminator { get; set; }
        public ulong AuthorId { get; set; }
        public string[] AttachmentUrls { get; set; }
        public string[] EmbedJsons { get; set; }
        public ulong[] MentionedChannelIds { get; set; }
        public ulong[] MentionedRoleIds { get; set; }
        public ulong[] MentionedUserIds { get; set; }
        public MessageActivity Activity { get; set; }
        public MessageApplication Application { get; set; }
        public ulong Reference { get; set; }
        public MessageFlags? Flags { get; set; }

        public static TicketTranscriptMessage FromMessage(IMessage msg)
        {
            return new TicketTranscriptMessage()
            {
                CreatedAt = msg.CreatedAt,
                Id = msg.Id,
                Type = msg.Type,
                Source = msg.Source,
                IsTTS = msg.IsTTS,
                IsPinned = msg.IsPinned,
                IsSuppressed = msg.IsSuppressed,
                MentionedEveryone = msg.MentionedEveryone,
                Content = msg.Content,
                CleanContent = msg.CleanContent,
                Timestamp = msg.Timestamp,
                EditedTimestamp = msg.EditedTimestamp,
                ChannelId = msg.Channel.Id,
                ChannelName = msg.Channel.Name,
                AuthorUsername = msg.Author.Username,
                AuthorDiscriminator = msg.Author.Discriminator,
                AuthorId = msg.Author.Id,
                AttachmentUrls = msg.Attachments.Select(v => v.Url).ToArray(),
                EmbedJsons = msg.Attachments.Select(v => JsonSerializer.Serialize(v, Program.SerializerOptions)).ToArray(),
                MentionedChannelIds = msg.MentionedChannelIds.ToArray(),
                MentionedRoleIds = msg.MentionedRoleIds.ToArray(),
                MentionedUserIds = msg.MentionedUserIds.ToArray(),
                Activity = msg.Activity,
                Application = msg.Application,
                Reference = msg.Reference?.MessageId.GetValueOrDefault() ?? 0,
                Flags = msg.Flags
            };
        }
    }
    public class TicketTranscriptModel
    {
        [Browsable(false)]
        public ObjectId _id { get; set; }
        public string Uid { get; set; }
        public string TicketUid { get; set; }
        public TicketTranscriptMessage[] Messages { get; set; }
        public TicketTranscriptModel()
        {
            Uid = kate.shared.Helpers.GeneralHelper.GenerateUID();
            TicketUid = "";
            Messages = Array.Empty<TicketTranscriptMessage>();
        }

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
}
