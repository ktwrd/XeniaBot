using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.WebSocket;
using SkidBot.Core.Helpers;

namespace SkidBot.Core.Controllers.Wrappers.BigBrother;

public enum BB_ChannelType
{
    Unknown = -1,
    Category,
    DM,
    Forum,
    Group,
    News,
    Stage,
    Text,
    Thread,
    Voice
}
public abstract class BB_ChannelModel
    : BigBrotherBaseModel
{
    public string Name { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsGuildChannel { get; set; }
    public bool IsDmChannel { get; set; }
    public bool IsForumChannel { get; set; }
    public BB_ChannelType Type { get; set; }
    
    public BB_ChannelModel FromExisting(SocketChannel channel)
    {
        Snowflake = channel.Id;
        CreatedAt = channel.CreatedAt;
        Type = BigBrotherHelper.GetChannelType(channel);
        return this;
    }
}

public class BB_DMChannelModel : BB_ChannelModel
{
    public BB_UserModel Recipient { get; set; }
    public BB_DMChannelModel FromExisting(SocketDMChannel channel)
    {
        base.FromExisting(channel);
        Name = channel.Recipient.Username;
        Recipient = BB_UserModel.FromUser(channel.Recipient);
        IsDmChannel = true;
        return this;
    }
}
public class BB_GuildChannelModel : BB_ChannelModel
{
    public BB_GuildModel Guild { get; set; }
    public string Name { get; set; }
    public int Position { get; set; }
    public ChannelFlags Flags { get; set; }
    public Overwrite[] PermissionOverwrite { get; set; }
    public BB_GuildChannelModel FromExisting(SocketGuildChannel channel)
    {
        base.FromExisting(channel);
        Guild = new BB_GuildModel().FromExisting(channel.Guild);
        Name = channel.Name;
        Position = channel.Position;
        Flags = channel.Flags;
        PermissionOverwrite = channel.PermissionOverwrites.ToArray();
        IsGuildChannel = true;
        return this;
    }
}
public class BB_ForumChannelModel : BB_ChannelModel
{
    public string Topic { get; set; }
    public bool IsNsfw { get; set; }
    public ThreadArchiveDuration DefaultAutoArchiveDuration { get; set; }
    public BB_ForumTag[] Tags { get; set; }
    public int ThreadCreationInterval { get; set; }
    public int DefaultSlowModeInterval { get; set; }
    public BB_Emote DefaultReactionEmoji { get; set; }
    public ForumSortOrder? DefaultSortOrder { get; set; }
    public ForumLayout DefaultLayout { get; set; }

    public BB_ForumChannelModel FromExisting(SocketForumChannel channel)
    {
        base.FromExisting(channel);
        IsNsfw = channel.IsNsfw;
        Topic = channel.Topic;
        DefaultAutoArchiveDuration = channel.DefaultAutoArchiveDuration;
        var tagList = new List<BB_ForumTag>();
        foreach (var i in channel.Tags)
        {
            tagList.Add(new BB_ForumTag().FromForumTag(i));
        }

        Tags = tagList.ToArray();
        ThreadCreationInterval = channel.ThreadCreationInterval;
        DefaultSlowModeInterval = channel.DefaultSlowModeInterval;
        DefaultReactionEmoji = new BB_Emote()
        {
            Name = channel.DefaultReactionEmoji.Name
        };
        DefaultSortOrder = channel.DefaultSortOrder;
        DefaultLayout = channel.DefaultLayout;
        IsForumChannel = true;
        return this;
    }
}
public class BB_ForumTag
{
    public ulong Id { get; set; }
    public string Name { get; set; }
    public BB_Emote? Emoji { get; set; }
    public bool IsModerated { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public BB_ForumTag FromForumTag(ForumTag tag)
    {
        Id = tag.Id;
        Name = tag.Name;
        if (tag.Emoji != null)
            Emoji = BigBrotherHelper.ForceTypeCast<IEmote, BB_Emote>(tag.Emoji);
        IsModerated = tag.IsModerated;
        CreatedAt = tag.CreatedAt;
        return this;
    }
}