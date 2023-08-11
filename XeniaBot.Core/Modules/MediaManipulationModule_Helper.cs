using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using XeniaBot.Shared;

namespace XeniaBot.Core.Modules;

public partial class MediaManipulationModule
{
    public static double[] WhiteRGBA => new double[] { 255, 255, 255, 255 };
    public static double[] WhiteRGB => new double[] { 255, 255, 255 };
    private async Task AttemptFontExtract()
    {
        if (!Directory.Exists(FeatureFlags.FontCache))
            Directory.CreateDirectory(FeatureFlags.FontCache);

        foreach (var pair in FontFilenamePairs)
        {
            var outputLocation = GetFontLocation(pair.Key);
            if (File.Exists(outputLocation))
                continue;
            var obj = MediaManipu.ResourceManager.GetObject(pair.Key) as byte[];
            using (var rs = new MemoryStream(obj))
            using (var fs = new FileStream(outputLocation, FileMode.Create, FileAccess.Write))
            {
                await rs.CopyToAsync(fs);
            }
        }
    }
    public static Dictionary<string, string> FontFilenamePairs => new Dictionary<string, string>()
    {
        {"font_arial", "arial.ttf"},
        {"font_AtkinsonHyperlegible_Bold", "AtkinsonHyperlegible-Bold.ttf"},
        {"font_caption", "caption.otf"},
        {"font_chirp_regular_web", "chip-regular-web.woff2"},
        {"font_HelveticaNeue", "HelveticaNeue.otf"},
        {"font_ImpactMix", "ImpactMix.ttf"},
        {"font_TAHOMABD", "TAHOMABD.TTF"},
        {"font_times_new_roman", "times new roman.ttf"},
        {"font_TwemojiCOLR0", "TwemojiCOLR0.otf"},
        {"font_Ubuntu_R", "Ubuntu-R.ttf"},
        {"font_whisper", "whisper.otf"}
    };
    private string GetFontLocation(string font)
    {
        return Path.Combine(FeatureFlags.FontCache, FontFilenamePairs[font]);
    }
}