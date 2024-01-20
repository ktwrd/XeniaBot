﻿using XeniaBot.DiscordCache.Helpers;
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
    
    public List<CacheMessageAttachment> Attachments { get; set; }
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
        Attachments = new List<CacheMessageAttachment>();
    }

    public CacheMessageModel? Clone()
    {
        return DiscordCacheHelper.ForceTypeCast<CacheMessageModel, CacheMessageModel>(this);
    }

    public CacheMessageModel Update(IMessage message)
    {
        this.Content = message.Content;
        this.Timestamp = message.Timestamp;
        this.EditedTimestamp = message.EditedTimestamp;
        this.Embeds = message.Embeds.Select(CacheMessageEmbed.FromExisting)
            .Where(v => v != null)
            .Cast<CacheMessageEmbed>()
            .ToArray();
        this.Tags = message.Tags.Select(CacheMessageTag.FromExisting)
            .Where(v => v != null)
            .Cast<CacheMessageTag>()
            .ToArray();
        this.Source = message.Source;
        this.IsTTS = message.IsTTS;
        this.Pinned = message.IsPinned;
        this.IsSuppressed = message.IsSuppressed;
        this.MentionedEveryone = message.MentionedEveryone;
        this.MentionedChannelIds = message.MentionedChannelIds.ToArray();
        this.MentionedRoleIds = message.MentionedRoleIds.ToArray();
        this.MentionedUserIds = message.MentionedUserIds.ToArray();
        this.Activity = CacheMessageActivity.FromExisting(message.Activity);
        this.Application = CacheMessageApplication.FromExisting(message.Application);
        this.Reference = CacheMessageReference.FromExisting(message.Reference);
        this.Components = message.Components
                .Select(CacheMessageComponent.FromExisting)
                .Where(v => v != null)
                .Cast<CacheMessageComponent>()
                .ToArray();
        this.Stickers = message.Stickers
            .Select(CacheStickerItem.FromExisting)
            .Where(v => v != null)
            .Cast<CacheStickerItem>()
            .ToArray();
        this.Flags = message.Flags;
        this.Interaction = CacheMessageInteraction.FromExisting(message.Interaction);
        this.CreatedAt = message.CreatedAt;
        this.Snowflake = message.Id;
        this.AuthorId = message.Author.Id;
        this.ChannelId = message.Channel.Id;
        this.GuildId = 0;
        this.Attachments = message.Attachments == null
            ? new List<CacheMessageAttachment>()
            : message.Attachments.Select(v => CacheMessageAttachment.FromExisting(v)).Where(v => v != null).ToList();
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