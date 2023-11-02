using XeniaBot.DiscordCache.Helpers;
using Discord;

namespace XeniaBot.DiscordCache.Models;

public class CacheMessageModel : DiscordCacheBaseModel
{
    #region IMessage
    public string Content { get; set; }
    public string ContentClean { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public DateTimeOffset? EditedTimestamp { get; set; }
    public CacheMessageEmbed[]? Embeds { get; set; }
    public CacheMessageTag[]? Tags { get; set; }
    public MessageSource Source { get; set; }
    public bool IsTTS { get; set; }
    public bool Pinned { get; set; }
    public bool IsSuppressed { get; set; }
    public bool MentionedEveryone { get; set; }

    public ulong[] MentionedChannelIds { get; set; }
    public ulong[] MentionedRoleIds { get; set; }
    public ulong[] MentionedUserIds { get; set; }
    public CacheMessageActivity? Activity { get; set; }
    public CacheMessageApplication? Application { get; set; }
    public CacheMessageReference? Reference { get; set; }
    public Dictionary<CacheEmote, CacheReactionMetadata> Reactions { get; set; }
    public CacheMessageComponent[] Components { get; set; }
    public CacheStickerItem[]? Stickers { get; set; }
    public MessageFlags? Flags { get; set; }
    
    public CacheMessageInteraction? Interaction { get; set; }
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
        Embeds = Array.Empty<CacheMessageEmbed>();
        Tags = Array.Empty<CacheMessageTag>();
        IsDeleted = false;
        DeletedTimestamp = DateTimeOffset.FromUnixTimeMilliseconds(0);
    }

    public CacheMessageModel? Clone()
    {
        return DiscordCacheHelper.ForceTypeCast<CacheMessageModel, CacheMessageModel>(this);
    }

    public static CacheMessageModel FromMessage(IMessage message)
    {
        var instance = new CacheMessageModel();
        instance.Content = message.Content;
        instance.Timestamp = message.Timestamp;
        instance.EditedTimestamp = message.EditedTimestamp;
        instance.Embeds = message.Embeds.Select(DiscordCacheHelper.ForceTypeCast<IEmbed, CacheMessageEmbed>)
            .Where(v => v != null)
            .Cast<CacheMessageEmbed>()
            .ToArray();
        instance.Tags = message.Tags.Select(DiscordCacheHelper.ForceTypeCast<ITag, CacheMessageTag>)
            .Where(v => v != null)
            .Cast<CacheMessageTag>()
            .ToArray();
        instance.Source = message.Source;
        instance.IsTTS = message.IsTTS;
        instance.Pinned = message.IsPinned;
        instance.IsSuppressed = message.IsSuppressed;
        instance.MentionedEveryone = message.MentionedEveryone;
        instance.MentionedChannelIds = message.MentionedChannelIds.ToArray();
        instance.MentionedRoleIds = message.MentionedRoleIds.ToArray();
        instance.MentionedUserIds = message.MentionedUserIds.ToArray();
        instance.Activity = DiscordCacheHelper.ForceTypeCast<MessageActivity, CacheMessageActivity>(message.Activity);
        instance.Application = DiscordCacheHelper.ForceTypeCast<MessageApplication, CacheMessageApplication>(message.Application);
        instance.Reference = CacheMessageReference.FromMessageReference(message.Reference);
        instance.Components = message.Components
                .Select(DiscordCacheHelper.ForceTypeCast<IMessageComponent, CacheMessageComponent>)
                .Where(v => v != null)
                .Cast<CacheMessageComponent>()
                .ToArray();
        instance.Stickers = message.Stickers
            .Select(DiscordCacheHelper.ForceTypeCast<IStickerItem, CacheStickerItem>)
            .Where(v => v != null)
            .Cast<CacheStickerItem>()
            .ToArray();
        instance.Flags = message.Flags;
        instance.Interaction = DiscordCacheHelper.ForceTypeCast<IMessageInteraction, CacheMessageInteraction>(message.Interaction);
        instance.CreatedAt = message.CreatedAt;
        instance.Snowflake = message.Id;
        instance.AuthorId = message.Author.Id;
        instance.ChannelId = message.Channel.Id;
        instance.GuildId = 0;
        return instance;
    }
}