using System.Globalization;
using Discord;
using Discord.WebSocket;

namespace XeniaBot.DiscordCache.Models;

public class CacheGuildModel
    : DiscordCacheBaseModel
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
    public string? VoiceRegionId { get; set; }
    public CacheAudioClient? AudioClient { get; set; }
    public CacheRole? EveryoneRole { get; set; }
    public CacheGuildEmote[] Emotes { get; set; }
    public CacheCustomSticker[] Stickers { get; set; }
    public GuildFeatures Features { get; set; }
    public CacheRole[] Roles { get; set; }
    public PremiumTier PremiumTier { get; set; }
    public string? BannerId { get; set; }
    public string? BannerUrl { get; set; }
    public string? VanityURLCode { get; set; }
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
    
    public CacheGuildModel FromExisting(SocketGuild guild)
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
        AudioClient = CacheAudioClient.FromExisting(guild.AudioClient);
        EveryoneRole = CacheRole.FromExisting(guild.EveryoneRole);
        Emotes = guild.Emotes
            .Select(CacheGuildEmote.FromExisting)
            .Where(v => v != null)
            .Cast<CacheGuildEmote>()
            .ToArray();
        Stickers = guild.Stickers
            .Select(CacheCustomSticker.FromExisting)
            .Where(v => v != null)
            .Cast<CacheCustomSticker>()
            .ToArray();
        Features = guild.Features;
        Roles = guild.Roles
            .Select(CacheRole.FromExisting)
            .Where(v => v != null)
            .Cast<CacheRole>()
            .ToArray();
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

    public static CacheGuildModel FromGuild(SocketGuild guild)
    {
        return new CacheGuildModel().FromExisting(guild);
    }
}