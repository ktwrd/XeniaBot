using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using System.Text.Json;
using System.Threading.Tasks;
using SkidBot.Core.Models;

namespace SkidBot.Core.Controllers.Wrappers.BigBrother;

public class BigBrotherBaseModel : BaseModel
{
    public ulong Snowflake;
}

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

public static class BigBrotherHelper
{
    public static TH? ForceTypeCast<T, TH>(T input)
    {
        var options = new JsonSerializerOptions()
        {
            IgnoreReadOnlyFields = true,
            IgnoreReadOnlyProperties = true,
            IncludeFields = true
        };
        var text = JsonSerializer.Serialize(input, options);
        var output = JsonSerializer.Deserialize<TH>(text, options);
        return output;
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
    public BB_User User { get; set; }

    public static BB_MessageInteraction FromInteraction(IMessageInteraction interaction)
    {
        var instance = new BB_MessageInteraction();
        instance.Id = interaction.Id;
        instance.Type = interaction.Type;
        instance.Name = interaction.Name;
        instance.User = BB_User.FromUser(interaction.User);
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

/// <summary>
/// This can be deserialized from <see cref="IEmbed"/> with <see cref="BB_MessageEmbed.FromEmbed()"/>
/// </summary>
public class BB_MessageEmbed
{
    public string Url { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public EmbedType Type { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public Discord.Color? Color { get; set; }
    public EmbedImage? Image { get; set; }
    public EmbedVideo? Video { get; set; }
    public BB_MessageEmbedAuthor? Author { get; set; }
    public BB_MessageEmbedFooter? Footer { get; set; }
    public BB_MessageEmbedProvider? Provider { get; set; }
    public BB_MessageEmbedThumbnail? Thumbnail { get; set; }
    public BB_MessageEmbedField[] Fields { get; set; }

    public BB_MessageEmbed()
    {
        Url = "";
        Title = "";
        Description = "";
        Fields = Array.Empty<BB_MessageEmbedField>();
    }
    public static BB_MessageEmbed? FromEmbed(IEmbed embed)
    {
        var opts = new JsonSerializerOptions()
        {
            IgnoreReadOnlyFields = true,
            IgnoreReadOnlyProperties = true,
            IncludeFields = true
        };
        var text = JsonSerializer.Serialize(embed, opts);
        return JsonSerializer.Deserialize<BB_MessageEmbed>(text, opts);
    }
}
#region Message Embed Fields
public class BB_MessageEmbedField
{
    public string Name { get; set; }
    public string Value { get; set; }
    public bool Inline { get; set; }

    public BB_MessageEmbedField()
    {
        Name = "";
        Value = "";
        Inline = false;
    }

    public bool Equals(BB_MessageEmbedField? embedField)
    {
        int hashCode1 = this.GetHashCode();
        int? hashCode2 = embedField?.GetHashCode();
        int valueOrDefault = hashCode2.GetValueOrDefault();
        return hashCode1 == valueOrDefault & hashCode2.HasValue;
    }

    public bool Equals(EmbedField? embedField)
    {
        int hashCode1 = this.GetHashCode();
        int? hashCode2 = embedField?.GetHashCode();
        int valueOrDefault = hashCode2.GetValueOrDefault();
        return hashCode1 == valueOrDefault & hashCode2.HasValue;
    }
}
public class BB_MessageEmbedThumbnail
{
    public string Url { get; set; }
    public string ProxyUrl { get; set; }
    public int? Height { get; set; }
    public int? Width { get; set; }

    public BB_MessageEmbedThumbnail()
    {
        Url = "";
        ProxyUrl = "";
    }
}
public class BB_MessageEmbedProvider
{
    public string Name { get; set; }
    public string Url { get; set; }

    public BB_MessageEmbedProvider()
    {
        Name = "";
        Url = "";
    }
}
public class BB_MessageEmbedFooter
{
    public string Text { get; set; }    
    public string IconUrl { get; set; }
    public string ProxyUrl { get; set; }

    public BB_MessageEmbedFooter()
    {
        Text = "";
        IconUrl = "";
        ProxyUrl = "";
    }
}
public class BB_MessageEmbedAuthor
{
    public string Name { get; set; }
    public string Url { get; set; }
    public string IconUrl { get; set; }
    public string ProxyIconUrl { get; set; }

    public BB_MessageEmbedAuthor()
    {
        Name = "";
        Url = "";
        IconUrl = "";
        ProxyIconUrl = "";
    }
}
#endregion




public class BB_User
    : BigBrotherBaseModel, IMentionable
{
    #region ISnowflakeEntity
    public DateTimeOffset CreatedAt { get; set; }
    #endregion
    
    #region IUser
    public string AvatarId { get; set; }
    public string Discriminator { get; set; }
    public ushort DiscriminatorValue { get; set; }
    
    public bool IsBot { get; set; }
    public bool IsWebhook { get; set; }
    public string Username { get; set; }
    
    public UserProperties? PublicFlags { get; set; }
    
    #region IMentionable
    public string Mention { get; set; }
    #endregion
    
    #region IPresence
    public UserStatus Status { get; set; }
    public ClientType[] ActiveClients { get; set; }
    public BB_Activity[] Activities { get; set; }
    #endregion

    /// <summary>
    /// Will always return empty string.
    /// </summary>
    /// <returns>Empty String</returns>
    public string GetAvatarUrl(ImageFormat format = ImageFormat.Auto, ushort size = 128)
    {
        return "";
    }

    /// <summary>
    /// Will always return empty string.
    /// </summary>
    /// <returns>Empty String</returns>
    public string GetDefaultAvatarUrl()
    {
        return "";
    }
    #endregion

    /// <summary>
    /// Always returns null.
    /// </summary>
    /// <returns>Null</returns>
    public async Task<IDMChannel> CreateDMChannelAsync(RequestOptions options = null)
    {
        return null;
    }

    public BB_User()
    {
        ActiveClients = Array.Empty<ClientType>();
        Activities = Array.Empty<BB_Activity>();
    }

    public static BB_User? FromUser(IUser? user)
    {
        if (user == null)
            return null;
        var instance = new BB_User();
        instance.Snowflake = user.Id;
        instance.CreatedAt = user.CreatedAt;
        instance.AvatarId = user.AvatarId;
        instance.Discriminator = user.Discriminator;
        instance.DiscriminatorValue = user.DiscriminatorValue;
        instance.IsBot = user.IsBot;
        instance.IsWebhook = user.IsWebhook;
        instance.Username = user.Username;
        instance.PublicFlags = user.PublicFlags;
        instance.Mention = user.Mention;
        instance.Status = user.Status;
        instance.ActiveClients = user.ActiveClients.ToArray();
        instance.Activities = BigBrotherHelper.ForceTypeCast<IReadOnlyCollection<IActivity>, BB_Activity[]>(user.Activities) ?? Array.Empty<BB_Activity>();
        return instance;
    }
}

public class BB_Activity : IActivity
{
    public string Name { get; set; }
    public ActivityType Type { get; set; }
    public ActivityProperties Flags { get; set; }
    public string Details { get; set; }
}