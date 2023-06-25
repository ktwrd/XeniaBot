using SkidBot.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkidBot.Core.Helpers
{
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
        public static ExperienceMetadata Generate(LevelMemberModel model)
        {
            var level = (ulong)Math.Floor(0.1 * Math.Sqrt(model.Xp));
            var levelStart = XpForLevel(level);
            var levelEnd = XpForLevel(level + 1);
            var levelSize = levelEnd - levelStart;
            var levelPerc = (model.Xp - levelStart) / (decimal)levelSize;
            var data = new ExperienceMetadata()
            {
                UserLevel = level,
                UserXp = model.Xp,
                NextLevelXp = XpForLevel(level + 1),
                CurrentLevelStart = levelStart,
                CurrentLevelEnd = levelEnd,
                CurrentLevelSize = levelEnd - levelStart,
                NextLevelProgress = Math.Round(levelPerc, 3)
            };
            return data;
        }
    }
}
