using System;
using XeniaBot.Shared.Models;

namespace XeniaBot.Data.Models;

public class GuildConfigWarnStrikeModel : BaseModelGuid
{
    public static string CollectionName => "guildConfig_warnStrike";
    public ulong GuildId { get; set; }
    /// <summary>
    /// Enable or Disable the Warn Strike system for this guild.
    /// </summary>
    public bool EnableStrikeSystem { get; set; }
    /// <summary>
    /// Maximum strikes allowed for a member.
    /// </summary>
    public int MaxStrike { get; set; }
    /// <summary>
    /// Automatically kick member when they have reached maximum amount of strikes.
    /// </summary>
    public bool AutoKickWhenMaxStrikeReached { get; set; }
    /// <summary>
    /// When a member in the server has reached the maximum amount of strikes allowed (defined in <see cref="MaxStrike"/>), then request from the moderators to kick this person.
    /// </summary>
    public bool RequestForKickWhenMaxStrikeReached { get; set; }
    /// <summary>
    /// How long we should check for warns to account for strikes. Measured in seconds.
    /// </summary>
    public double StrikeWindow { get; set; }

    /// <summary>
    /// Format <see cref="StrikeWindow"/> to Years/Days
    /// </summary>
    public string FormatStrikeWindow()
    {
        var years = Math.Floor(StrikeWindow / 365f);
        var days = StrikeWindow % 365;
        var result = "";
        if (years > 0)
            result += $"{years} year";
        result += years > 1 ? "s " : "";

        if (days > 0)
            result += $"{days} day";
        if (days > 1)
            result += "s";
        return result;
    }
    /// <summary>
    /// Unix Timestamp (UTC, Seconds)
    /// </summary>
    public long UpdatedAt { get; set; }

    public GuildConfigWarnStrikeModel()
    {
        GuildId = 0;
        EnableStrikeSystem = false;
        MaxStrike = 3;
        AutoKickWhenMaxStrikeReached = false;
        RequestForKickWhenMaxStrikeReached = true;
        StrikeWindow = TimeSpan.FromDays(31).TotalSeconds;
        UpdatedAt = 0;
    }
}