using Discord;
using Discord.WebSocket;
using System.ComponentModel.DataAnnotations;

namespace XeniaDiscord.Data.Models.Snapshot;

public class GuildSnapshotModel
{
    public const string TableName = "Snapshot_Guild";

    public GuildSnapshotModel()
    {
        RecordId = Guid.NewGuid();
        RecordCreatedAt = DateTime.UtcNow;
        GuildId = "0";
        CreatedAt = DateTimeOffset.UnixEpoch.UtcDateTime;
        
        Name = "";
        OwnerUserId = "0";
        EveryoneRoleId = "0";

        PreferredLocale = "en";
    }

    public void Update(IGuild guild)
    {
        GuildId = guild.Id.ToString();
        CreatedAt = guild.CreatedAt.UtcDateTime;
        Name = guild.Name;
        Description = guild.Description?.Trim();
        OwnerUserId = guild.OwnerId.ToString();
        EveryoneRoleId = guild.EveryoneRole.Id.ToString();
        IconUrl = guild.IconUrl?.Trim();
        IconId = guild.IconId?.Trim();
        BannerUrl = guild.BannerUrl?.Trim();
        BannerId = guild.BannerId?.Trim();
        SplashUrl = guild.SplashUrl?.Trim();
        SplashId = guild.SplashId?.Trim();
        DiscoverySplashUrl = guild.DiscoverySplashUrl?.Trim();
        DiscoverySplashId = guild.DiscoverySplashId?.Trim();
        VanityUrlCode = guild.VanityURLCode?.Trim();
        AfkTimeout = guild.AFKTimeout;

        DefaultMessageNotifications = guild.DefaultMessageNotifications;
        MfaLevel = guild.MfaLevel;
        VerificationLevel = guild.VerificationLevel;
        ExplicitContentFilter = guild.ExplicitContentFilter;
        NsfwLevel = guild.NsfwLevel;
        GuildFeatures = guild.Features?.Value ?? GuildFeature.None;

        PremiumSubscriptionCount = guild.PremiumSubscriptionCount;
        IsBoostProgressBarEnabled = guild.IsBoostProgressBarEnabled;
        MaxPresences = guild.MaxPresences;
        MaxMembers = guild.MaxMembers;
        MaxVideoChannelUsers = guild.MaxVideoChannelUsers;
        MaxStageVideoChannelUsers = guild.MaxStageVideoChannelUsers;
        ApproximateMemberCount = guild.ApproximateMemberCount;
        ApproximatePresenceCount = guild.ApproximatePresenceCount;
        MaxBitrate = guild.MaxBitrate;
        MaxUploadLimit = guild.MaxUploadLimit;
        PreferredLocale = guild.PreferredLocale?.Trim() ?? "en";
        VoiceRegionId = guild.VoiceRegionId?.ToString();

        AfkChannelId = guild.AFKChannelId?.ToString();
        WidgetChannelId = guild.WidgetChannelId?.ToString();
        SafetyAlertsChannelId = guild.SafetyAlertsChannelId?.ToString();
        SystemChannelId = guild.SystemChannelId?.ToString();
        RulesChannelId = guild.RulesChannelId?.ToString();
        PublicUpdatesChannelId = guild.PublicUpdatesChannelId?.ToString();
        ApplicationId = guild.ApplicationId?.ToString();

        if (guild is SocketGuild socketGuild)
        {
            JoinedAt = socketGuild.CurrentUser.JoinedAt?.UtcDateTime;
        }

        if (string.IsNullOrEmpty(Description)) Description = null;
        if (string.IsNullOrEmpty(IconUrl)) IconUrl = null;
        if (string.IsNullOrEmpty(IconId)) IconId = null;
        if (string.IsNullOrEmpty(BannerUrl)) BannerUrl = null;
        if (string.IsNullOrEmpty(BannerId)) BannerId = null;
        if (string.IsNullOrEmpty(SplashUrl)) SplashUrl = null;
        if (string.IsNullOrEmpty(SplashId)) SplashId = null;
        if (string.IsNullOrEmpty(DiscoverySplashUrl)) DiscoverySplashUrl = null;
        if (string.IsNullOrEmpty(DiscoverySplashId)) DiscoverySplashId = null;
    }

    public void Update(GuildSnapshotModel model)
    {
        GuildId = model.GuildId;
        CreatedAt = model.CreatedAt;
        JoinedAt = model.JoinedAt;
        Name = model.Name;
        Description = model.Description?.Trim();
        OwnerUserId = model.OwnerUserId;
        EveryoneRoleId = model.EveryoneRoleId;
        IconUrl = model.IconUrl;
        IconId = model.IconId;
        BannerUrl = model.BannerUrl;
        BannerId = model.BannerId;
        SplashUrl = model.SplashUrl;
        SplashId = model.SplashId;
        DiscoverySplashUrl = model.DiscoverySplashUrl;
        DiscoverySplashId = model.DiscoverySplashId;
        VanityUrlCode = model.VanityUrlCode;
        AfkTimeout = model.AfkTimeout;
        DefaultMessageNotifications = model.DefaultMessageNotifications;
        MfaLevel = model.MfaLevel;
        VerificationLevel = model.VerificationLevel;
        ExplicitContentFilter = model.ExplicitContentFilter;
        NsfwLevel = model.NsfwLevel;
        GuildFeatures = model.GuildFeatures;
        PremiumSubscriptionCount = model.PremiumSubscriptionCount;
        IsBoostProgressBarEnabled = model.IsBoostProgressBarEnabled;
        MaxPresences = model.MaxPresences;
        MaxMembers = model.MaxMembers;
        MaxVideoChannelUsers = model.MaxVideoChannelUsers;
        MaxStageVideoChannelUsers = model.MaxStageVideoChannelUsers;
        ApproximateMemberCount = model.ApproximateMemberCount;
        ApproximatePresenceCount = model.ApproximatePresenceCount;
        MaxBitrate = model.MaxBitrate;
        MaxUploadLimit = model.MaxUploadLimit;
        PreferredLocale = model.PreferredLocale;
        VoiceRegionId = model.VoiceRegionId;
        AfkChannelId = model.AfkChannelId;
        WidgetChannelId = model.WidgetChannelId;
        SafetyAlertsChannelId = model.SafetyAlertsChannelId;
        SystemChannelId = model.SystemChannelId;
        RulesChannelId = model.RulesChannelId;
        PublicUpdatesChannelId = model.PublicUpdatesChannelId;
        ApplicationId = model.ApplicationId;
    }

    /// <summary>
    /// Record Id (primary key)
    /// </summary>
    public Guid RecordId { get; set; }
    /// <summary>
    /// UTC Time of when this record was created.
    /// </summary>
    public DateTime RecordCreatedAt { get; set; }
    public DiscordSnapshotSource SnapshotSource { get; set; } 
    /// <summary>
    /// Guild Id (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string GuildId { get; set; }
    /// <summary>
    /// UTC Time of when the guild was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>
    /// UTC Time of when the bot joined this guild.
    /// This value will be null when they're not a member.
    /// </summary>
    public DateTime? JoinedAt { get; set; }

    /// <summary>
    /// Value of: <see cref="IGuild.Name"/>
    /// </summary>
    [MaxLength(100)]
    public string Name { get; set; }
    /// <summary>
    /// Value of: <see cref="IGuild.Description"/>
    /// </summary>
    [MaxLength(2000)]
    public string? Description { get; set; }
    /// <summary>
    /// <see cref="IGuild.OwnerId"/> as string
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string OwnerUserId { get; set; }
    /// <summary>
    /// <see cref="IGuild.EveryoneRole"/> (id, ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string EveryoneRoleId { get; set; }

    /// <summary>
    /// URL to the Guild Icon
    /// </summary>
    [MaxLength(500)]
    public string? IconUrl { get; set; }
    [MaxLength(100)]
    /// <summary><see cref="IGuild.IconId"/></summary>
    public string? IconId { get; set; }
    /// <summary>
    /// URL to the Banner Image
    /// </summary>
    [MaxLength(500)]
    public string? BannerUrl { get; set; }
    /// <summary><see cref="IGuild.BannerId"/></summary>
    [MaxLength(100)]
    public string? BannerId { get; set; }
    /// <summary>
    /// URL to the Splash Image
    /// </summary>
    [MaxLength(500)]
    public string? SplashUrl { get; set; }
    /// <summary><see cref="IGuild.SplashId"/></summary>
    [MaxLength(100)]
    public string? SplashId { get; set; }
    /// <summary>
    /// URL to the Splash Image used in Discord Discovery
    /// </summary>
    [MaxLength(500)]
    public string? DiscoverySplashUrl { get; set; }
    /// <summary><see cref="IGuild.DiscoverySplashId"/></summary>
    [MaxLength(100)]
    public string? DiscoverySplashId { get; set; }
    /// <summary>
    /// <inheritdoc cref="IGuild.VanityUrlCode"/>
    /// </summary>
    [MaxLength(20)]
    public string? VanityUrlCode { get; set; }

    /// <summary>
    /// <inheritdoc cref="IGuild.AfkTimeout"/>
    /// </summary>
    public int AfkTimeout { get; set; }

    /// <summary>
    /// <inheritdoc cref="IGuild.DefaultMessageNotifications"/>
    /// </summary>
    public DefaultMessageNotifications DefaultMessageNotifications { get; set; }
    /// <summary>
    /// <inheritdoc cref="IGuild.MfaLevel"/>
    /// </summary>
    public MfaLevel MfaLevel { get; set; }
    /// <summary>
    /// <inheritdoc cref="IGuild.VerificationLevel"/>
    /// </summary>
    public VerificationLevel VerificationLevel { get; set; }
    /// <summary>
    /// <inheritdoc cref="IGuild.ExplicitContentFilter"/>
    /// </summary>
    public ExplicitContentFilterLevel ExplicitContentFilter { get; set; }

    /// <summary>
    /// <inheritdoc cref="IGuild.NsfwLevel"/>
    /// </summary>
    public NsfwLevel NsfwLevel { get; set; }

    /// <summary>
    /// <see cref="GuildFeatures.Value"/> from <see cref="IGuild.Features"/>
    /// </summary>
    public GuildFeature GuildFeatures { get; set; }

    /// <summary>
    /// <inheritdoc cref="IGuild.PremiumSubscriptionCount"/>
    /// </summary>
    public int PremiumSubscriptionCount { get; set; }
    /// <summary>
    /// <inheritdoc cref="IGuild.IsBoostProgressBarEnabled"/>
    /// </summary>
    public bool IsBoostProgressBarEnabled { get; set; }

    /// <summary>
    /// <inheritdoc cref="IGuild.MaxPresences"/>
    /// </summary>
    public int? MaxPresences { get; set; }
    /// <summary>
    /// <inheritdoc cref="IGuild.MaxMembers"/>
    /// </summary>
    public int? MaxMembers { get; set; }
    /// <summary>
    /// <inheritdoc cref="IGuild.MaxVideoChannelUsers"/>
    /// </summary>
    public int? MaxVideoChannelUsers { get; set; }
    /// <summary>
    /// <inheritdoc cref="IGuild.MaxStageVideoChannelUsers"/>
    /// </summary>
    public int? MaxStageVideoChannelUsers { get; set; }
    /// <summary>
    /// <inheritdoc cref="IGuild.ApproximateMemberCount"/>
    /// </summary>
    public int? ApproximateMemberCount { get; set; }
    /// <summary>
    /// <inheritdoc cref="IGuild.ApproximatePresenceCount"/>
    /// </summary>
    public int? ApproximatePresenceCount { get; set; }
    /// <summary>
    /// <inheritdoc cref="IGuild.MaxBitrate"/>
    /// </summary>
    public int MaxBitrate { get; set; }
    /// <summary>
    /// <inheritdoc cref="IGuild.MaxUploadLimit"/>
    /// </summary>
    public ulong MaxUploadLimit { get; set; }

    /// <summary>
    /// <inheritdoc cref="IGuild.PreferredLocale"/>
    /// </summary>
    [MaxLength(20)]
    public string PreferredLocale { get; set; }
    /// <summary>
    /// <inheritdoc cref="IGuild.VoiceRegionId"/>
    /// </summary>
    [MaxLength(20)]
    public string? VoiceRegionId { get; set; }

    /// <summary>
    /// <see cref="IGuild.AFKChannelId"/> as string
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string? AfkChannelId { get; set; }
    /// <summary>
    /// <see cref="IGuild.WidgetChannelId"/> as string
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string? WidgetChannelId { get; set; }
    /// <summary>
    /// <see cref="IGuild.SafetyAlertsChannelId"/> as string
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string? SafetyAlertsChannelId { get; set; }
    /// <summary>
    /// <see cref="IGuild.SystemChannelId"/> as string
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string? SystemChannelId { get; set; }
    /// <summary>
    /// <see cref="IGuild.RulesChannelId"/> as string
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string? RulesChannelId { get; set; }
    /// <summary>
    /// <see cref="IGuild.PublicUpdatesChannelId"/> as string
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string? PublicUpdatesChannelId { get; set; }
    /// <summary>
    /// <see cref="IGuild.ApplicationId"/> as string
    /// </summary>
    [MaxLength(DbGlobals.ulongMaxLength)]
    public string? ApplicationId { get; set; }

    public ulong GetGuildId() => GuildId.ParseRequiredULong(nameof(GuildId), false);
    public ulong GetOwnerUserId() => OwnerUserId.ParseRequiredULong(nameof(OwnerUserId), false);
    public ulong GetEveryoneRoleId() => EveryoneRoleId.ParseRequiredULong(nameof(EveryoneRoleId), false);
    public ulong? GetAfkChannelId() => AfkChannelId.ParseULong(false);
    public ulong? GetWidgetChannelId() => WidgetChannelId.ParseULong(false);
    public ulong? GetSafetyAlertsChannelId() => SafetyAlertsChannelId.ParseULong(false);
    public ulong? GetSystemChannelId() => SystemChannelId.ParseULong(false);
    public ulong? GetPublicUpdatesChannelId() => PublicUpdatesChannelId.ParseULong(false);
    public ulong? GetApplicationId() => ApplicationId.ParseULong(false);
}
