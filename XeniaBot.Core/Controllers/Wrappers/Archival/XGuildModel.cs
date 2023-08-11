using System;
using System.Globalization;
using System.Linq;
using Discord;
using Discord.Audio;
using Discord.WebSocket;

namespace XeniaBot.Core.Controllers.Wrappers.Archival;

public class XGuildModel
    : ArchiveBaseModel
{
    public DateTimeOffset CreatedAt { get; set; }
    public string Name { get; set; }
    public int AFKTimeout { get; set; }
    public bool IsWidgetEnabled { get; set; }
    public DefaultMessageNotifications DefaultMessageNotifications { get; set; }
    public MfaLevel MfaLevel { get; set; }
    public VerificationLevel VerificationLevel { get; set; }
    public ExplicitContentFilterLevel ExplicitContentFilter { get; set; }
    public string IconId { get; set; }
    public string SplashId { get; set; }
    public string SplashUrl { get; set; }
    public string DiscoverySplashId { get; set; }
    public string DiscoverySplashUrl { get; set; }
    public ulong? AFKChannelId { get; set; }
    public ulong? WidgetChannelId { get; set; }
    public ulong? SystemChannelId { get; set; }
    public ulong? RulesChannelId { get; set; }
    public ulong? PublicUpdatesChannelId { get; set; }
    public ulong OwnerId { get; set; }
    public ulong? ApplicationId { get; set; }
    public string VoiceRegionId { get; set; }
    public XAudioClient AudioClient { get; set; }
    public XRole EveryoneRole { get; set; }
    public XGuildEmote[] Emotes { get; set; }
    public XCustomSticker[] Stickers { get; set; }
    public GuildFeatures Features { get; set; }
    public XRole[] Roles { get; set; }
    public PremiumTier PremiumTier { get; set; }
    public string BannerId { get; set; }
    public string BannerUrl { get; set; }
    public string VanityURLCode { get; set; }
    public SystemChannelMessageDeny SystemChannelFlags { get; set; }
    public string Description { get; set; }
    public int PremiumSubscriptionCount { get; set; }
    public int? MaxPresences { get; set; }
    public int? MaxMembers { get; set; }
    public int? MaxVideoChannelUsers { get; set; }
    public int? MemberCount { get; set; }
    public int MaxBitrate { get; set; }
    public string PreferredLocale { get; set; }
    public NsfwLevel NsfwLevel { get; set; }
    public CultureInfo PreferredCulture { get; set; }
    public bool IsBoostProgressBarEnabled { get; set; }
    public ulong MaxUploadLimit { get; set; }
    
    public XGuildModel FromExisting(SocketGuild guild)
    {
        Snowflake = guild.Id;
        CreatedAt = guild.CreatedAt;
        Name = guild.Name;
        AFKTimeout = guild.AFKTimeout;
        IsWidgetEnabled = guild.IsWidgetEnabled;
        DefaultMessageNotifications = guild.DefaultMessageNotifications;
        MfaLevel = guild.MfaLevel;
        VerificationLevel = guild.VerificationLevel;
        ExplicitContentFilter = guild.ExplicitContentFilter;
        IconId = guild.IconId;
        SplashId = guild.SplashId;
        SplashUrl = guild.SplashUrl;
        DiscoverySplashId = guild.DiscoverySplashId;
        DiscoverySplashUrl = guild.DiscoverySplashUrl;
        AFKChannelId = guild.AFKChannel?.Id ?? 0;
        WidgetChannelId = guild.WidgetChannel?.Id ?? 0;
        SystemChannelId = guild.SystemChannel?.Id ?? 0;
        RulesChannelId = guild.RulesChannel?.Id ?? 0;
        PublicUpdatesChannelId = guild.PublicUpdatesChannel?.Id ?? 0;
        OwnerId = guild.OwnerId;
        ApplicationId = guild.ApplicationId;
        VoiceRegionId = guild.VoiceRegionId;
        AudioClient = new XAudioClient().FromExisting(guild.AudioClient);
        EveryoneRole = new XRole().FromExisting(guild.EveryoneRole);
        Emotes = guild.Emotes.Select(v => new XGuildEmote().FromExisting(v)).ToArray();
        Stickers = guild.Stickers.Select(v => new XCustomSticker().FromExisting(v)).ToArray();
        Features = guild.Features;
        Roles = guild.Roles.Select(v => new XRole().FromExisting(v)).ToArray();
        PremiumTier = guild.PremiumTier;
        BannerId = guild.BannerId;
        BannerUrl = guild.BannerUrl;
        VanityURLCode = guild.VanityURLCode;
        SystemChannelFlags = guild.SystemChannelFlags;
        Description = guild.Description;
        PremiumSubscriptionCount = guild.PremiumSubscriptionCount;
        MaxPresences = guild.MaxPresences;
        MaxMembers = guild.MaxMembers;
        MaxVideoChannelUsers = guild.MaxVideoChannelUsers;
        MemberCount = guild.MemberCount;
        MaxBitrate = guild.MaxBitrate;
        PreferredLocale = guild.PreferredLocale;
        NsfwLevel = guild.NsfwLevel;
        PreferredCulture = guild.PreferredCulture;
        IsBoostProgressBarEnabled = guild.IsBoostProgressBarEnabled;
        MaxUploadLimit = guild.MaxUploadLimit;
        return this;
    }
}

public class XRole
{
    public ulong GuildId { get; set; }
    public Discord.Color Color { get; set; }
    public bool IsHoisted { get; set; }
    public bool IsManaged { get; set; }
    public bool IsMentionable { get; set; }
    public string Name { get; set; }
    public string Icon { get; set; }
    public XEmote Emoji { get; set; }
    public GuildPermissions Permissions { get; set; }
    public int Position { get; set; }
    public RoleTags Tags { get; set; }

    public XRole FromExisting(IRole role)
    {
        GuildId = role.Guild.Id;
        Color = role.Color;
        IsHoisted = role.IsHoisted;
        IsManaged = role.IsManaged;
        IsMentionable = role.IsMentionable;
        Name = role.Name;
        Icon = role.Icon;
        Emoji = new XEmote().FromExisting(role.Emoji);
        Permissions = role.Permissions;
        Position = role.Position;
        Tags = role.Tags;
        return this;
    }
}

public class XAudioClient
{
    public ConnectionState ConnectionState { get; set; }
    public int Latency { get; set; }
    public int UdpLatency { get; set; }
    public XAudioClient FromExisting(IAudioClient audioClient)
    {
        ConnectionState = audioClient.ConnectionState;
        Latency = audioClient.Latency;
        UdpLatency = audioClient.UdpLatency;
        return this;
    }
}

public class XGuildEmote : XEmote
{
    public bool IsManaged { get; set; }
    public bool RequireColons { get; set; }
    public ulong[] RoleIds { get; set; }
    public ulong? CreatorId { get; set; }

    public XGuildEmote FromExisting(GuildEmote emote)
    {
        base.FromExisting(emote);
        IsManaged = emote.IsManaged;
        RequireColons = emote.RequireColons;
        RoleIds = emote.RoleIds.ToArray();
        CreatorId = emote.CreatorId;
        return this;
    }
}

public class XCustomSticker : XSticker
{
    public ulong? AuthorId { get; set; }
    public ulong GuildId { get; set; }

    public XCustomSticker FromExisting(ICustomSticker sticker)
    {
        base.FromExisting(sticker);
        AuthorId = sticker.AuthorId;
        GuildId = sticker.Guild.Id;
        return this;
    }
}

public class XSticker : XStickerItem
{
    public ulong Id { get; set; }
    public ulong PackId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string[] Tags { get; set; }
    public StickerType Type { get; set; }
    public StickerFormatType Format { get; set; }
    public bool? IsAvailable { get; set; }
    public int? SortOrder { get; set; }

    public XSticker FromExisting(ISticker sticker)
    {
        base.FromExisting(sticker);
        Id = sticker.Id;
        PackId = sticker.PackId;
        Name = sticker.Name;
        Description = sticker.Description;
        Tags = sticker.Tags.ToArray();
        Type = sticker.Type;
        Format = sticker.Format;
        IsAvailable = sticker.IsAvailable;
        SortOrder = sticker.SortOrder;
        return this;
    }
}