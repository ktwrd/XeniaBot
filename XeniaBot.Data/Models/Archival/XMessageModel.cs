using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using XeniaBot.Data.Helpers;

namespace XeniaBot.Data.Models.Archival;


public class XMessageModel : ArchiveBaseModel
{
    #region IMessage
    public string Content { get; set; }
    public string ContentClean { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public DateTimeOffset? EditedTimestamp { get; set; }
    public XMessageEmbed[] Embeds { get; set; }
    public XMessageTag[] Tags { get; set; }
    public MessageSource Source { get; set; }
    public bool IsTTS { get; set; }
    public bool Pinned { get; set; }
    public bool IsSuppressed { get; set; }
    public bool MentionedEveryone { get; set; }

    public ulong[] MentionedChannelIds { get; set; }
    public ulong[] MentionedRoleIds { get; set; }
    public ulong[] MentionedUserIds { get; set; }
    public XMessageActivity Activity { get; set; }
    public XMessageApplication Application { get; set; }
    public XMessageReference Reference { get; set; }
    public Dictionary<XEmote, XReactionMetadata> Reactions { get; set; }
    public XMessageComponent[] Components { get; set; }
    public XStickerItem[] Stickers { get; set; }
    public MessageFlags? Flags { get; set; }
    
    public XMessageInteraction Interaction { get; set; }
    #endregion
    
    #region ISnowflakeEntity
    public DateTimeOffset CreatedAt { get; set; }
    #endregion
    
    public ulong AuthorId { get; set; }
    public ulong ChannelId { get; set; }
    public ulong GuildId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset DeletedTimestamp { get; set; }

    public XMessageModel()
    {
        Content = "";
        ContentClean = "";
        Embeds = Array.Empty<XMessageEmbed>();
        Tags = Array.Empty<XMessageTag>();
        IsDeleted = false;
        DeletedTimestamp = DateTimeOffset.FromUnixTimeMilliseconds(0);
    }

    public XMessageModel? Clone()
    {
        return ArchivalHelper.ForceTypeCast<XMessageModel, XMessageModel>(this);
    }

    public static XMessageModel FromMessage(IMessage message)
    {
        var instance = new XMessageModel();
        instance.Content = message.Content;
        instance.ContentClean = message.CleanContent;
        instance.Timestamp = message.Timestamp;
        instance.EditedTimestamp = message.EditedTimestamp;
        instance.Embeds = message.Embeds.Select(v => ArchivalHelper.ForceTypeCast<IEmbed, XMessageEmbed>(v)).ToArray();
        instance.Tags = message.Tags.Select(v => ArchivalHelper.ForceTypeCast<ITag, XMessageTag>(v)).ToArray();
        instance.Source = message.Source;
        instance.IsTTS = message.IsTTS;
        instance.Pinned = message.IsPinned;
        instance.IsSuppressed = message.IsSuppressed;
        instance.MentionedEveryone = message.MentionedEveryone;
        instance.MentionedChannelIds = message.MentionedChannelIds.ToArray();
        instance.MentionedRoleIds = message.MentionedRoleIds.ToArray();
        instance.MentionedUserIds = message.MentionedUserIds.ToArray();
        instance.Activity = ArchivalHelper.ForceTypeCast<MessageActivity, XMessageActivity>(message.Activity);
        instance.Application = ArchivalHelper.ForceTypeCast<MessageApplication, XMessageApplication>(message.Application);
        instance.Reference = XMessageReference.FromMessageReference(message.Reference);
        instance.Components = message.Components
                .Select(v => ArchivalHelper.ForceTypeCast<IMessageComponent, XMessageComponent>(v))
                .Where(v => v != null)
                .Cast<XMessageComponent>()
                .ToArray();
        instance.Stickers = message.Stickers
                .Select(v => ArchivalHelper.ForceTypeCast<IStickerItem, XStickerItem>(v))
                .Where(v => v != null)
                .ToArray();
        instance.Flags = message.Flags;
        instance.Interaction = ArchivalHelper.ForceTypeCast<IMessageInteraction, XMessageInteraction>(message.Interaction);
        instance.CreatedAt = message.CreatedAt;
        instance.Snowflake = message.Id;
        instance.AuthorId = message.Author.Id;
        instance.ChannelId = message.Channel.Id;
        instance.GuildId = 0;
        return instance;
    }
}

#region Message Fields
public class XEmote
{
    public string Name { get; set; }

    public XEmote FromExisting(IEmote emote)
    {
        Name = emote.Name;
        return this;
    }
}

public class XReactionMetadata
{
    public int ReactionCount { get; set; }
    public bool IsMe { get; set; }
}
public class XMessageTag : ITag
{
    public int Index { get; set; }
    public int Length { get; set; }
    public TagType Type { get; set; }
    public ulong Key { get; set; }
    public object Value { get; set; }
}

public class XMessageComponent : IMessageComponent
{
    public ComponentType Type { get; set; }
    public string CustomId { get; set; }

    public XMessageComponent()
    {
        CustomId = "";
    }
}

public class XMessageInteraction
{
    public ulong Id { get; set; }
    public InteractionType Type { get; set; }
    public string Name { get; set; }
    public XUserModel User { get; set; }

    public static XMessageInteraction FromInteraction(IMessageInteraction interaction)
    {
        var instance = new XMessageInteraction();
        instance.Id = interaction.Id;
        instance.Type = interaction.Type;
        instance.Name = interaction.Name;
        instance.User = XUserModel.FromUser(interaction.User);
        return instance;
    }
}

public class XStickerItem : IStickerItem
{
    public ulong Id { get; set; }
    public string Name { get; set; }
    public StickerFormatType Format { get; set; }

    public XStickerItem FromExisting(IStickerItem sticker)
    {
        Id = sticker.Id;
        Name = sticker.Name;
        Format = sticker.Format;
        return this;
    }
}

/// <summary>
/// Same as <see cref="MessageActivity"/>
/// </summary>
public class XMessageActivity
{
    public new MessageActivityType Type { get; set; }
    public new string PartyId { get; set; }

    public XMessageActivity()
    {
        PartyId = "";
    }
}

public class XMessageApplication
{
    public ulong Id { get; set; }
    public string CoverImage { get; set; }
    public string Description { get; set; }
    public string Icon { get; set; }
    public string IconUrl { get; set; }
    public string Name { get; set; }
}

public class XMessageReference
{
    public ulong MessageId { get; set; }
    public ulong ChannelID { get; set; }
    public ulong GuildId { get; set; }
    public bool FailIfNotExists { get; set; }

    public static XMessageReference FromMessageReference(MessageReference? r)
    {
        return new XMessageReference()
        {
            MessageId = (r?.MessageId.IsSpecified ?? false) ? r?.MessageId.Value ?? 0 : 0,
            ChannelID = r != null && r?.ChannelId != null ? r?.ChannelId ?? 0 : 0,
            GuildId = (r?.GuildId.IsSpecified ?? false) ? r?.GuildId.Value ?? 0 : 0,
            FailIfNotExists = (r?.FailIfNotExists.IsSpecified ?? false) ? true : r?.FailIfNotExists.Value ?? true
        };
    }
}
#endregion
