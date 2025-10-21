using Discord;
using System.ComponentModel.DataAnnotations;

namespace XeniaDiscord.Data.Models.DiscordSnapshot;

public class DiscordSnapshotGuildModel
{
    public const string TableName = "DiscordSnapshotGuild";
    public DiscordSnapshotGuildModel()
    {
        Id = Guid.NewGuid();
        SnapshotTimestamp = DateTimeOffset.UtcNow;

        GuildId = "0";
    }

    public Guid Id { get; set; }

    /// <summary>
    /// Time when this snapshot was created
    /// </summary>
    public DateTimeOffset SnapshotTimestamp { get; set; }

    /// <summary>
    /// Discord Guild Snowflake (ulong as string)
    /// </summary>
    [MaxLength(DbGlobals.MaxLength.ULong)]
    public string GuildId { get; set; }

    public string Name { get; set; }
    public int AfkTimeout { get; set; }
    public bool IsWidgetEnabled { get; set; }
    public DefaultMessageNotifications DefaultMessageNotifications { get; set; }
    public MfaLevel MfaLevel { get; set; }
    public VerificationLevel VerificationLevel { get; set; }
    public string? IconId { get; set; }
    public string? IconUrl { get; set; }
    public string? SplashId { get; set; }
    public string? SplashUrl { get; set; }
    public string? DiscoverySplashId { get; set; }
    public string? DiscoverySplashUrl { get; set; }
    /// <summary>
    /// Discord Channel Snowflake
    /// </summary>
    [MaxLength(DbGlobals.MaxLength.ULong)]
    public string? AfkChannelId { get; set; } // ulong as string
    /// <summary>
    /// Discord Channel Snowflake
    /// </summary>
    [MaxLength(DbGlobals.MaxLength.ULong)]
    public string? WidgetChannelId { get; set; } // ulong as string
    /// <summary>
    /// Discord Channel Snowflake
    /// </summary>
    [MaxLength(DbGlobals.MaxLength.ULong)]
    public string? SafetyAlertsChannelId { get; set; } // ulong as string
    /// <summary>
    /// Discord Channel Snowflake
    /// </summary>
    [MaxLength(DbGlobals.MaxLength.ULong)]
    public string? SystemChannelId { get; set; } // ulong as string
    /// <summary>
    /// Discord Channel Snowflake
    /// </summary>
    [MaxLength(DbGlobals.MaxLength.ULong)]
    public string? RulesChannelId { get; set; } // ulong as string
    /// <summary>
    /// Discord Channel Snowflake
    /// </summary>
    [MaxLength(DbGlobals.MaxLength.ULong)]
    public string? PublicUpdatesChannelId { get; set; } // ulong as string
    /// <summary>
    /// Discord User Snowflake
    /// </summary>
    [MaxLength(DbGlobals.MaxLength.ULong)]
    public string OwnerId { get; set; } // ulong as string
    /// <summary>
    /// Discord Application Snowflake
    /// </summary>
    [MaxLength(DbGlobals.MaxLength.ULong)]
    public string? ApplicationId { get; set; } // ulong as string
    public string? VoiceRegionId { get; set; }
    /// <summary>
    /// Discord Role Snowflake
    /// </summary>
    [MaxLength(DbGlobals.MaxLength.ULong)]
    public string? EveryoneRoleId { get; set; } // ulong as string
    // Emotes
    // Stickers
    // Features
    public string? BannerId { get; set; }
    public string? BannerUrl { get; set; }
    public string? VanityUrlCode { get; set; }
    public SystemChannelMessageDeny SystemChannelFlags { get; set; }
    public string? Description { get; set; }
    public int PremiumSubscriptionCount { get; set; }
    public int? MaxPresences { get; set; }
    public int? MaxMembers { get; set; }
    public int? MaxVideoChannelUsers { get; set; }
    public int? ApproximateMemberCount { get; set; }
    public int? ApproximatePresenceCount { get; set; }
    public int MaxBitrate { get; set; }
    public string? PreferredLocale { get; set; }
    public NsfwLevel NsfwLevel { get; set; }
    public bool IsBoostProgressBarEnabled { get; set; }
    [MaxLength(DbGlobals.MaxLength.ULong)]
    public string MaxUploadLimit { get; set; } // ulong as string
    // InventorySettings
    // IncidentsData
}
