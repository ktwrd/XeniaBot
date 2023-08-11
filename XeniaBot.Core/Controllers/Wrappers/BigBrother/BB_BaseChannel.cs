using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.WebSocket;
using XeniaBot.Core.Helpers;

namespace XeniaBot.Core.Controllers.Wrappers.BigBrother;

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

public class BB_ChannelModel : BigBrotherBaseModel
{
    public BB_ChannelType Type { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public BB_DMChannelModel? DMChannel { get; set; }
    public BB_GuildChannelModel? GuildChannel { get; set; }
    public BB_ForumChannelModel? ForumChannel { get; set; }
    public BB_GroupChannelModel? GroupChannel { get; set; }
    public BB_TextChannelModel? TextChannel { get; set; }
    public BB_TextChannelModel? NewsChannel { get; set; }
    public BB_ThreadChannelModel? ThreadChannel { get; set; }
    public BB_VoiceChannelModel? VoiceChannel { get; set; }
    public BB_StageChannelModel? StageChannel { get; set; }
    public void Generate(SocketChannel channel)
    {
        Type = BigBrotherHelper.GetChannelType(channel);
        Snowflake = channel.Id;
        CreatedAt = channel.CreatedAt;
        if (channel is SocketDMChannel dmChannel)
            DMChannel = new BB_DMChannelModel().FromExisting(dmChannel);
        if (channel is SocketForumChannel forumChannel)
            ForumChannel = new BB_ForumChannelModel().FromExisting(forumChannel);
        if (channel is SocketGroupChannel groupChannel)
            GroupChannel = new BB_GroupChannelModel().FromExisting(groupChannel);

        if (channel is SocketStageChannel stageChannel)
            StageChannel = new BB_StageChannelModel().FromExisting(stageChannel);
        if (channel is SocketVoiceChannel voiceChannel)
            VoiceChannel = new BB_VoiceChannelModel().FromExisting(voiceChannel);
        if (channel is SocketNewsChannel newsChannel)
            NewsChannel = new BB_TextChannelModel().FromExisting(newsChannel);
        if (channel is SocketThreadChannel threadChannel)
            ThreadChannel = new BB_ThreadChannelModel().FromExisting(threadChannel);
        if (channel is SocketTextChannel textChannel)
            TextChannel = new BB_TextChannelModel().FromExisting(textChannel);

        
        if (channel is SocketGuildChannel guildChannel)
            GuildChannel = new BB_GuildChannelModel().FromExisting(guildChannel);
    }
}
public abstract class BB_BaseChannel
{
    public ulong Snowflake { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsGuildChannel { get; set; }
    public bool IsDmChannel { get; set; }
    public bool IsForumChannel { get; set; }
    public BB_ChannelType Type { get; set; }
    
    public BB_BaseChannel FromExisting(SocketChannel channel)
    {
        Snowflake = channel.Id;
        CreatedAt = channel.CreatedAt;
        Type = BigBrotherHelper.GetChannelType(channel);
        return this;
    }
}

public class BB_StageChannelModel : BB_VoiceChannelModel
{
    public StagePrivacyLevel? PrivacyLevel { get; set; }
    public bool? IsDiscoverableDisabled { get; set; }
    public bool IsLive { get; set; }
    public ulong[] SpeakerIds { get; set; }
    public new BB_StageChannelModel FromExisting(SocketStageChannel channel)
    {
        base.FromExisting(channel);
        PrivacyLevel = channel.PrivacyLevel;
        IsDiscoverableDisabled = channel.IsDiscoverableDisabled;
        IsLive = channel.IsLive;
        SpeakerIds = channel.Speakers.Select(v => v.Id).ToArray();
        return this;
    }
}
public class BB_VoiceChannelModel : BB_TextChannelModel
{
    public int Bitrate { get; set; }
    public int? UserLimit { get; set; }
    public string RTCRegion { get; set; }
    public ulong[] ConnectedUserIds { get; set; }
    public new BB_VoiceChannelModel FromExisting(SocketVoiceChannel channel)
    {
        base.FromExisting(channel);
        Bitrate = channel.Bitrate;
        UserLimit = channel.UserLimit;
        RTCRegion = channel.RTCRegion;
        ConnectedUserIds = channel.ConnectedUsers.Select(v => v.Id).ToArray();
        return this;
    }
}
public class BB_ThreadChannelModel : BB_TextChannelModel
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

    public BB_ThreadChannelModel FromExisting(SocketThreadChannel channel)
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
public class BB_TextChannelModel : BB_GuildChannelModel
{
    public string Topic { get; set; }
    public int SlowModeInterval { get; set; }
    public ulong? CategoryId { get; set; }
    public bool IsNsfw { get; set; }
    public ThreadArchiveDuration DefaultArchiveDuration { get; set; }
    public string Mention { get; set; }
    public ulong[] ThreadIds { get; set; }
    public new BB_TextChannelModel FromExisting(SocketTextChannel channel)
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
public class BB_GroupChannelModel : BB_BaseChannel
{
    public string Name { get; set; }
    public string RTCRegion { get; set; }
    public ulong[] UserIds { get; set; }
    public ulong[] RecipientIds { get; set; }

    public BB_GroupChannelModel FromExisting(SocketGroupChannel channel)
    {
        base.FromExisting(channel);
        Name = channel.Name;
        RTCRegion = channel.RTCRegion;
        UserIds = channel.Users.Select(v => v.Id).ToArray();
        RecipientIds = channel.Recipients.Select(v => v.Id).ToArray();
        return this;
    }
}
public class BB_DMChannelModel : BB_BaseChannel
{
    public string Name { get; set; }
    public BB_UserModel Recipient { get; set; }
    public new BB_DMChannelModel FromExisting(SocketDMChannel channel)
    {
        base.FromExisting(channel);
        Name = channel.Recipient.Username;
        Recipient = BB_UserModel.FromUser(channel.Recipient);
        IsDmChannel = true;
        return this;
    }
}
public class BB_GuildChannelModel : BB_BaseChannel
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
public class BB_ForumChannelModel : BB_BaseChannel
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