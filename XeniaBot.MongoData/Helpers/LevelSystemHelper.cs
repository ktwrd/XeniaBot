using System;
using XeniaBot.MongoData.Models;

namespace XeniaBot.MongoData.Helpers;

public delegate void ExperienceComparisonDelegate(LevelMemberModel model, ExperienceMetadata previous, ExperienceMetadata current);
public class ExperienceMetadata
{
    public ulong UserLevel { get; set; }
    public ulong UserXp { get; set; }
    public ulong NextLevelXp { get; set; }
    public ulong CurrentLevelStart { get; set; }
    public ulong CurrentLevelEnd { get; set; }
    public ulong CurrentLevelSize { get; set; }
    public decimal NextLevelProgress { get; set; }
}
public static class LevelSystemHelper
{
    public const int XpPerLevel = 100;
    public static ulong XpForLevel(ulong level)
    {
        return level * level * 100;
    }
    public static ExperienceMetadata Generate(ulong xp)
    {
        var level = (ulong)Math.Floor(0.1 * Math.Sqrt(xp));
        var levelStart = XpForLevel(level);
        var levelEnd = XpForLevel(level + 1);
        var levelSize = levelEnd - levelStart;
        var levelPerc = (xp - levelStart) / (decimal)levelSize;
        var data = new ExperienceMetadata()
        {
            UserLevel = level,
            UserXp = xp,
            NextLevelXp = XpForLevel(level + 1),
            CurrentLevelStart = levelStart,
            CurrentLevelEnd = levelEnd,
            CurrentLevelSize = levelEnd - levelStart,
            NextLevelProgress = Math.Round(levelPerc, 3)
        };
        return data;
    }

    public static ExperienceMetadata Generate(LevelMemberModel model) => Generate(model.Xp);
}
