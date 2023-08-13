using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.WebSocket;
using XeniaBot.Data.Helpers;

namespace XeniaBot.Data.Models.Archival;

public enum XChannelType
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

public class XChannelModel : ArchiveBaseModel
{
    public XChannelType Type { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public XDmChannelModel? DMChannel { get; set; }
    public XGuildChannelModel? GuildChannel { get; set; }
    public XForumChannelModel? ForumChannel { get; set; }
    public XGroupChannelModel? GroupChannel { get; set; }
    public XTextChannelModel? TextChannel { get; set; }
    public XTextChannelModel? NewsChannel { get; set; }
    public XThreadChannelModel? ThreadChannel { get; set; }
    public XVoiceChannelModel? VoiceChannel { get; set; }
    public XStageChannelModel? StageChannel { get; set; }
    public void Generate(SocketChannel channel)
    {
        Type = ArchivalHelper.GetChannelType(channel);
        Snowflake = channel.Id;
        CreatedAt = channel.CreatedAt;
        if (channel is SocketDMChannel dmChannel)
            DMChannel = new XDmChannelModel().FromExisting(dmChannel);
        if (channel is SocketForumChannel forumChannel)
            ForumChannel = new XForumChannelModel().FromExisting(forumChannel);
        if (channel is SocketGroupChannel groupChannel)
            GroupChannel = new XGroupChannelModel().FromExisting(groupChannel);

        if (channel is SocketStageChannel stageChannel)
            StageChannel = new XStageChannelModel().FromExisting(stageChannel);
        if (channel is SocketVoiceChannel voiceChannel)
            VoiceChannel = new XVoiceChannelModel().FromExisting(voiceChannel);
        if (channel is SocketNewsChannel newsChannel)
            NewsChannel = new XTextChannelModel().FromExisting(newsChannel);
        if (channel is SocketThreadChannel threadChannel)
            ThreadChannel = new XThreadChannelModel().FromExisting(threadChannel);
        if (channel is SocketTextChannel textChannel)
            TextChannel = new XTextChannelModel().FromExisting(textChannel);

        
        if (channel is SocketGuildChannel guildChannel)
            GuildChannel = new XGuildChannelModel().FromExisting(guildChannel);
    }
}
public abstract class XBaseChannel
{
    public ulong Snowflake { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsGuildChannel { get; set; }
    public bool IsDmChannel { get; set; }
    public bool IsForumChannel { get; set; }
    public XChannelType Type { get; set; }
    
    public XBaseChannel FromExisting(SocketChannel channel)
    {
        Snowflake = channel.Id;
        CreatedAt = channel.CreatedAt;
        Type = ArchivalHelper.GetChannelType(channel);
        return this;
    }
}

public class XStageChannelModel : XVoiceChannelModel
{
    public StagePrivacyLevel? PrivacyLevel { get; set; }
    public bool? IsDiscoverableDisabled { get; set; }
    public bool IsLive { get; set; }
    public ulong[] SpeakerIds { get; set; }
    public new XStageChannelModel FromExisting(SocketStageChannel channel)
    {
        base.FromExisting(channel);
        PrivacyLevel = channel.PrivacyLevel;
        IsDiscoverableDisabled = channel.IsDiscoverableDisabled;
        IsLive = channel.IsLive;
        SpeakerIds = channel.Speakers.Select(v => v.Id).ToArray();
        return this;
    }
}
public class XVoiceChannelModel : XTextChannelModel
{
    public int Bitrate { get; set; }
    public int? UserLimit { get; set; }
    public string RTCRegion { get; set; }
    public ulong[] ConnectedUserIds { get; set; }
    public new XVoiceChannelModel FromExisting(SocketVoiceChannel channel)
    {
        base.FromExisting(channel);
        Bitrate = channel.Bitrate;
        UserLimit = channel.UserLimit;
        RTCRegion = channel.RTCRegion;
        ConnectedUserIds = channel.ConnectedUsers.Select(v => v.Id).ToArray();
        return this;
    }
}
public class XThreadChannelModel : XTextChannelModel
{
    public ThreadType Type { get; set; }
    public ulong OwnerId { get; set; }
    public bool HasJoined { get; set; }
    public bool IsPrivateThread { get; set; }
    public ulong ParentChannelId { get; set; }
    public int MessageCount { get; set; }
    public int MemberCount { get; set; }
    public bool IsArchived { get; set; }
    public DateTimeOffset ArchiveTimestamp { get; set; }
    public ThreadArchiveDuration AutoArchiveDuration { get; set; }
    public bool IsLocked { get; set; }
    public bool? IsInvitable { get; set; }
    public ulong[] AppliedTags { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public ulong[] UserIds { get; set; }

    public XThreadChannelModel FromExisting(SocketThreadChannel channel)
    {
        base.FromExisting(channel);
        Type = channel.Type;
        OwnerId = channel.Owner.Id;
        HasJoined = channel.HasJoined;
        IsPrivateThread = channel.IsPrivateThread;
        ParentChannelId = channel.ParentChannel.Id;
        MessageCount = channel.MessageCount;
        MemberCount = channel.MemberCount;
        IsArchived = channel.IsArchived;
        ArchiveTimestamp = channel.ArchiveTimestamp;
        AutoArchiveDuration = channel.AutoArchiveDuration;
        IsLocked = channel.IsLocked;
        IsInvitable = channel.IsInvitable;
        AppliedTags = channel.AppliedTags.ToArray();
        CreatedAt = channel.CreatedAt;
        UserIds = channel.Users.Select(v => v.Id).ToArray();
        return this;
    }
}
public class XTextChannelModel : XGuildChannelModel
{
    public string Topic { get; set; }
    public int SlowModeInterval { get; set; }
    public ulong? CategoryId { get; set; }
    public bool IsNsfw { get; set; }
    public ThreadArchiveDuration DefaultArchiveDuration { get; set; }
    public string Mention { get; set; }
    public ulong[] ThreadIds { get; set; }
    public new XTextChannelModel FromExisting(SocketTextChannel channel)
    {
        base.FromExisting(channel);
        Topic = channel.Topic;
        SlowModeInterval = channel.SlowModeInterval;
        CategoryId = channel.CategoryId;
        IsNsfw = channel.IsNsfw;
        DefaultArchiveDuration = channel.DefaultArchiveDuration;
        Mention = channel.Mention;
        ThreadIds = channel.Threads.Select(v => v.Id).ToArray();
        return this;
    }
}
public class XGroupChannelModel : XBaseChannel
{
    public string Name { get; set; }
    public string RTCRegion { get; set; }
    public ulong[] UserIds { get; set; }
    public ulong[] RecipientIds { get; set; }

    public XGroupChannelModel FromExisting(SocketGroupChannel channel)
    {
        base.FromExisting(channel);
        Name = channel.Name;
        RTCRegion = channel.RTCRegion;
        UserIds = channel.Users.Select(v => v.Id).ToArray();
        RecipientIds = channel.Recipients.Select(v => v.Id).ToArray();
        return this;
    }
}
public class XDmChannelModel : XBaseChannel
{
    public string Name { get; set; }
    public XUserModel Recipient { get; set; }
    public new XDmChannelModel FromExisting(SocketDMChannel channel)
    {
        base.FromExisting(channel);
        Name = channel.Recipient.Username;
        Recipient = XUserModel.FromUser(channel.Recipient);
        IsDmChannel = true;
        return this;
    }
}
public class XGuildChannelModel : XBaseChannel
{
    public XGuildModel Guild { get; set; }
    public string Name { get; set; }
    public int Position { get; set; }
    public ChannelFlags Flags { get; set; }
    public Overwrite[] PermissionOverwrite { get; set; }
    public XGuildChannelModel FromExisting(SocketGuildChannel channel)
    {
        base.FromExisting(channel);
        Guild = new XGuildModel().FromExisting(channel.Guild);
        Name = channel.Name;
        Position = channel.Position;
        Flags = channel.Flags;
        PermissionOverwrite = channel.PermissionOverwrites.ToArray();
        IsGuildChannel = true;
        return this;
    }
}
public class XForumChannelModel : XBaseChannel
{
    public string Topic { get; set; }
    public bool IsNsfw { get; set; }
    public ThreadArchiveDuration DefaultAutoArchiveDuration { get; set; }
    public XForumTag[] Tags { get; set; }
    public int ThreadCreationInterval { get; set; }
    public int DefaultSlowModeInterval { get; set; }
    public XEmote DefaultReactionEmoji { get; set; }
    public ForumSortOrder? DefaultSortOrder { get; set; }
    public ForumLayout DefaultLayout { get; set; }

    public XForumChannelModel FromExisting(SocketForumChannel channel)
    {
        base.FromExisting(channel);
        IsNsfw = channel.IsNsfw;
        Topic = channel.Topic;
        DefaultAutoArchiveDuration = channel.DefaultAutoArchiveDuration;
        var tagList = new List<XForumTag>();
        foreach (var i in channel.Tags)
        {
            tagList.Add(new XForumTag().FromForumTag(i));
        }

        Tags = tagList.ToArray();
        ThreadCreationInterval = channel.ThreadCreationInterval;
        DefaultSlowModeInterval = channel.DefaultSlowModeInterval;
        DefaultReactionEmoji = new XEmote()
        {
            Name = channel.DefaultReactionEmoji.Name
        };
        DefaultSortOrder = channel.DefaultSortOrder;
        DefaultLayout = channel.DefaultLayout;
        IsForumChannel = true;
        return this;
    }
}
public class XForumTag
{
    public ulong Id { get; set; }
    public string Name { get; set; }
    public XEmote? Emoji { get; set; }
    public bool IsModerated { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public XForumTag FromForumTag(ForumTag tag)
    {
        Id = tag.Id;
        Name = tag.Name;
        if (tag.Emoji != null)
            Emoji = ArchivalHelper.ForceTypeCast<IEmote, XEmote>(tag.Emoji);
        IsModerated = tag.IsModerated;
        CreatedAt = tag.CreatedAt;
        return this;
    }
}