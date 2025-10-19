using System;
using XeniaBot.Data.Models;

namespace XeniaBot.Data.Helpers;

public delegate void ExperienceComparisonDelegate(LevelMemberModel model, ExperienceMetadata previous, ExperienceMetadata current);
public class ExperienceMetadata
{
    public ulong UserLevel;
    public ulong UserXp;
    public ulong NextLevelXp;
    public ulong CurrentLevelStart;
    public ulong CurrentLevelEnd;
    public ulong CurrentLevelSize;
    public decimal NextLevelProgress;
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
