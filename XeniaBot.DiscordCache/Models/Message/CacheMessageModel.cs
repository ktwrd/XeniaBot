using Discord;
using Discord.WebSocket;
using MongoDB.Bson.Serialization.Attributes;

namespace XeniaBot.DiscordCache.Models;

public class CacheMessageModel : DiscordCacheBaseModel
{
    public static string CollectionName => "bb_store_message";
    #region IMessage
    public string Content { get; set; }
    public string ContentClean { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    [BsonIgnoreIfNull]
    public DateTimeOffset? EditedTimestamp { get; set; }
    [BsonIgnoreIfNull]
    public CacheMessageEmbed[]? Embeds { get; set; }
    [BsonIgnoreIfNull]
    public CacheMessageTag[]? Tags { get; set; }
    public MessageSource Source { get; set; }
    public bool IsTTS { get; set; }
    public bool Pinned { get; set; }
    public bool IsSuppressed { get; set; }
    public bool MentionedEveryone { get; set; }

    [BsonIgnoreIfNull]
    public ulong[]? MentionedChannelIds { get; set; }
    [BsonIgnoreIfNull]
    public ulong[]? MentionedRoleIds { get; set; }
    [BsonIgnoreIfNull]
    public ulong[]? MentionedUserIds { get; set; }
    [BsonIgnoreIfNull]
    public CacheMessageActivity? Activity { get; set; }
    [BsonIgnoreIfNull]
    public CacheMessageApplication? Application { get; set; }
    [BsonIgnoreIfNull]
    public CacheMessageReference? Reference { get; set; }
    [BsonIgnoreIfNull]
    public Dictionary<CacheEmote, CacheReactionMetadata>? Reactions { get; set; }
    [BsonIgnoreIfNull]
    public CacheMessageComponent[]? Components { get; set; }
    [BsonIgnoreIfNull]
    public CacheStickerItem[]? Stickers { get; set; }
    [BsonIgnoreIfNull]
    public MessageFlags? Flags { get; set; }
    [BsonIgnoreIfNull]
    public CacheMessageInteraction? Interaction { get; set; }
    [BsonIgnoreIfNull]
    public CacheMessageInteractionMetadata? InteractionMetadata { get; set; }
    [BsonIgnoreIfNull]
    public List<CacheMessageAttachment>? Attachments { get; set; }
    #endregion

    #region ISnowflakeEntity
    public DateTimeOffset CreatedAt { get; set; }
    #endregion

    public ulong AuthorId { get; set; }
    public ulong ChannelId { get; set; }
    public ulong GuildId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset DeletedTimestamp { get; set; }

    public CacheMessageModel()
    {
        Content = "";
        ContentClean = "";
        IsDeleted = false;
        DeletedTimestamp = DateTimeOffset.FromUnixTimeMilliseconds(0);
    }

    public CacheMessageModel Update(IMessage message)
    {
        Content = message.Content;
        Timestamp = message.Timestamp;
        EditedTimestamp = message.EditedTimestamp;
        Embeds = message.Embeds.Count == 0 ? null
            : message.Embeds.Select(CacheMessageEmbed.FromExisting)
                .Where(v => v != null)
                .Cast<CacheMessageEmbed>()
                .ToArray();
        Tags = message.Tags.Count == 0 ? null
            : message.Tags.Select(CacheMessageTag.FromExisting)
                .Where(v => v != null)
                .Cast<CacheMessageTag>()
                .ToArray();
        Source = message.Source;
        IsTTS = message.IsTTS;
        Pinned = message.IsPinned;
        IsSuppressed = message.IsSuppressed;
        MentionedEveryone = message.MentionedEveryone;
        MentionedChannelIds = message.MentionedChannelIds.Count == 0
            ? null : message.MentionedChannelIds.ToArray();
        MentionedRoleIds = message.MentionedRoleIds.Count == 0
            ? null : message.MentionedRoleIds.ToArray();
        MentionedUserIds = message.MentionedUserIds.Count == 0
            ? null : message.MentionedUserIds.ToArray();
        Activity = CacheMessageActivity.FromExisting(message.Activity);
        Application = CacheMessageApplication.FromExisting(message.Application);
        Reference = CacheMessageReference.FromExisting(message.Reference);
        Components = message.Components.Count == 0 ? null
            : message.Components
                .Select(CacheMessageComponent.FromExisting)
                .Where(v => v != null)
                .Cast<CacheMessageComponent>()
                .ToArray();
        Stickers = message.Stickers.Count == 0 ? null
            : message.Stickers
                .Select(CacheStickerItem.FromExisting)
                .Where(v => v != null)
                .Cast<CacheStickerItem>()
                .ToArray();
        Flags = message.Flags;
        if (message is IUserMessage usrMsg)
        {
            InteractionMetadata = CacheMessageInteractionMetadata.FromExisting(usrMsg.InteractionMetadata);
        }
        else
        {
            InteractionMetadata = null;
        }
        Interaction = null;
        CreatedAt = message.CreatedAt;
        Snowflake = message.Id;
        AuthorId = message.Author.Id;
        ChannelId = message.Channel.Id;
        GuildId = 0;

        if (message.Channel is SocketGuildChannel sgc)
        {
            GuildId = sgc.Guild.Id;
        }

        Attachments = message.Attachments == null || message.Attachments.Count < 1
            ? null
            : message.Attachments.Select(CacheMessageAttachment.FromExisting)
                .Where(e => e != null)
                .Cast<CacheMessageAttachment>().ToList();
        return this;
    }
    public static CacheMessageModel? FromExisting(IMessage? message)
    {
        if (message == null)
            return null;

        var instance = new CacheMessageModel();
        return instance.Update(message);
    }
}