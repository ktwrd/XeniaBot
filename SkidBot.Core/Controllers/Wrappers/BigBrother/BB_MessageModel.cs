using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using SkidBot.Core.Helpers;

namespace SkidBot.Core.Controllers.Wrappers.BigBrother;


public class BB_MessageModel : BigBrotherBaseModel
{
    #region IMessage
    public string Content { get; set; }
    public string ContentClean { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public DateTimeOffset? EditedTimestamp { get; set; }
    public BB_MessageEmbed[] Embeds { get; set; }
    public BB_MessageTag[] Tags { get; set; }
    public MessageSource Source { get; set; }
    public bool IsTTS { get; set; }
    public bool Pinned { get; set; }
    public bool IsSuppressed { get; set; }
    public bool MentionedEveryone { get; set; }

    public ulong[] MentionedChannelIds { get; set; }
    public ulong[] MentionedRoleIds { get; set; }
    public ulong[] MentionedUserIds { get; set; }
    public BB_MessageActivity Activity { get; set; }
    public BB_MessageApplication Application { get; set; }
    public BB_MessageReference Reference { get; set; }
    public Dictionary<BB_Emote, BB_ReactionMetadata> Reactions { get; set; }
    public BB_MessageComponent[] Components { get; set; }
    public BB_StickerItem[] Stickers { get; set; }
    public MessageFlags? Flags { get; set; }
    
    public BB_MessageInteraction Interaction { get; set; }
    #endregion
    
    #region ISnowflakeEntity
    public DateTimeOffset CreatedAt { get; set; }
    #endregion
    
    public ulong AuthorId { get; set; }
    public ulong ChannelId { get; set; }
    public ulong GuildId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset DeletedTimestamp { get; set; }

    public BB_MessageModel()
    {
        Content = "";
        ContentClean = "";
        Embeds = Array.Empty<BB_MessageEmbed>();
        Tags = Array.Empty<BB_MessageTag>();
        IsDeleted = false;
        DeletedTimestamp = DateTimeOffset.FromUnixTimeMilliseconds(0);
    }

    public BB_MessageModel? Clone()
    {
        return BigBrotherHelper.ForceTypeCast<BB_MessageModel, BB_MessageModel>(this);
    }

    public static BB_MessageModel FromMessage(IMessage message)
    {
        var instance = new BB_MessageModel();
        instance.Content = message.Content;
        instance.ContentClean = message.CleanContent;
        instance.Timestamp = message.Timestamp;
        instance.EditedTimestamp = message.EditedTimestamp;
        instance.Embeds = message.Embeds.Select(v => BigBrotherHelper.ForceTypeCast<IEmbed, BB_MessageEmbed>(v)).ToArray();
        instance.Tags = message.Tags.Select(v => BigBrotherHelper.ForceTypeCast<ITag, BB_MessageTag>(v)).ToArray();
        instance.Source = message.Source;
        instance.IsTTS = message.IsTTS;
        instance.Pinned = message.IsPinned;
        instance.IsSuppressed = message.IsSuppressed;
        instance.MentionedEveryone = message.MentionedEveryone;
        instance.MentionedChannelIds = message.MentionedChannelIds.ToArray();
        instance.MentionedRoleIds = message.MentionedRoleIds.ToArray();
        instance.MentionedUserIds = message.MentionedUserIds.ToArray();
        instance.Activity = BigBrotherHelper.ForceTypeCast<MessageActivity, BB_MessageActivity>(message.Activity);
        instance.Application = BigBrotherHelper.ForceTypeCast<MessageApplication, BB_MessageApplication>(message.Application);
        instance.Reference = BB_MessageReference.FromMessageReference(message.Reference);
        instance.Components = message.Components
                .Select(v => BigBrotherHelper.ForceTypeCast<IMessageComponent, BB_MessageComponent>(v))
                .Where(v => v != null)
                .Cast<BB_MessageComponent>()
                .ToArray();
        instance.Stickers = message.Stickers
                .Select(v => BigBrotherHelper.ForceTypeCast<IStickerItem, BB_StickerItem>(v))
                .Where(v => v != null)
                .ToArray();
        instance.Flags = message.Flags;
        instance.Interaction = BigBrotherHelper.ForceTypeCast<IMessageInteraction, BB_MessageInteraction>(message.Interaction);
        instance.CreatedAt = message.CreatedAt;
        instance.Snowflake = message.Id;
        instance.AuthorId = message.Author.Id;
        instance.ChannelId = message.Channel.Id;
        instance.GuildId = 0;
        return instance;
    }
}

#region Message Fields
public class BB_Emote
{
    public string Name { get; set; }
}

public class BB_ReactionMetadata
{
    public int ReactionCount { get; set; }
    public bool IsMe { get; set; }
}
public class BB_MessageTag : ITag
{
    public int Index { get; set; }
    public int Length { get; set; }
    public TagType Type { get; set; }
    public ulong Key { get; set; }
    public object Value { get; set; }
}

public class BB_MessageComponent : IMessageComponent
{
    public ComponentType Type { get; set; }
    public string CustomId { get; set; }

    public BB_MessageComponent()
    {
        CustomId = "";
    }
}

public class BB_MessageInteraction
{
    public ulong Id { get; set; }
    public InteractionType Type { get; set; }
    public string Name { get; set; }
    public BB_UserModel User { get; set; }

    public static BB_MessageInteraction FromInteraction(IMessageInteraction interaction)
    {
        var instance = new BB_MessageInteraction();
        instance.Id = interaction.Id;
        instance.Type = interaction.Type;
        instance.Name = interaction.Name;
        instance.User = BB_UserModel.FromUser(interaction.User);
        return instance;
    }
}

public class BB_StickerItem : IStickerItem
{
    public ulong Id { get; set; }
    public string Name { get; set; }
    public StickerFormatType Format { get; set; }
}

/// <summary>
/// Same as <see cref="MessageActivity"/>
/// </summary>
public class BB_MessageActivity
{
    public new MessageActivityType Type { get; set; }
    public new string PartyId { get; set; }

    public BB_MessageActivity()
    {
        PartyId = "";
    }
}

public class BB_MessageApplication
{
    public ulong Id { get; set; }
    public string CoverImage { get; set; }
    public string Description { get; set; }
    public string Icon { get; set; }
    public string IconUrl { get; set; }
    public string Name { get; set; }
}

public class BB_MessageReference
{
    public ulong MessageId { get; set; }
    public ulong ChannelID { get; set; }
    public ulong GuildId { get; set; }
    public bool FailIfNotExists { get; set; }

    public static BB_MessageReference FromMessageReference(MessageReference? r)
    {
        return new BB_MessageReference()
        {
            MessageId = (r?.MessageId.IsSpecified ?? false) ? r?.MessageId.Value ?? 0 : 0,
            ChannelID = r != null && r?.ChannelId != null ? r?.ChannelId ?? 0 : 0,
            GuildId = (r?.GuildId.IsSpecified ?? false) ? r?.GuildId.Value ?? 0 : 0,
            FailIfNotExists = (r?.FailIfNotExists.IsSpecified ?? false) ? true : r?.FailIfNotExists.Value ?? true
        };
    }
}
#endregion
