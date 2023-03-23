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
        public double NextLevelProgress;
    }
    public static class LevelSystemHelper
    {
        public const int XpPerLevel = 100;
        public static ExperienceMetadata Generate(LevelMemberModel model)
        {
            ulong level = 0;
            ulong xp = model.Xp;
            ulong targetXp = 0;  // xp for next level
            double progress = 0; // % until next level

            while (true)
            {
                var size = XpPerLevel / 2 * (level ^ 2);
                var floor = XpPerLevel / 2 * level;
                targetXp = floor + size;

                if (xp < targetXp)
                {
                    double perc = (xp - floor) / (double)size;
                    progress = Math.Round(perc, 3);
                    return new ExperienceMetadata()
                    {
                        UserLevel = level,
                        UserXp = xp,
                        NextLevelXp = targetXp,
                        NextLevelProgress = progress,
                        CurrentLevelStart = floor,
                        CurrentLevelEnd = targetXp,
                        CurrentLevelSize = size
                    };
                }
                level += 1;
            }
        }
    }
}
