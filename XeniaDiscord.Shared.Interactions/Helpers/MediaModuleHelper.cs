using Discord;
using XeniaBot.Shared;
using XeniaBot.Shared.Helpers;

namespace XeniaDiscord.Shared.Interactions.Helpers;

public static class MediaModuleHelper
{
    public static async Task AttemptFontExtract()
    {
        if (!Directory.Exists(FeatureFlags.FontCache))
            Directory.CreateDirectory(FeatureFlags.FontCache);

        foreach (var pair in FontFilenamePairs)
        {
            var outputLocation = GetFontLocation(pair.Key);
            if (File.Exists(outputLocation)) continue;
            var resourceName = "XeniaDiscord.Shared.Interactions.Resources.MediaModule." + pair.Value;
            await using var stream = ResourceHelper.GetStream(resourceName);
            await using var fs = new FileStream(outputLocation, FileMode.Create, FileAccess.Write);
            await stream.CopyToAsync(fs);
        }
    }
    public static string GetFontLocation(string font)
    {
        return Path.Combine(FeatureFlags.FontCache, FontFilenamePairs[font]);
    }
    public static IReadOnlyDictionary<string, string> FontFilenamePairs => new Dictionary<string, string>()
    {
        {"font_arial", "arial.ttf"},
        {"font_AtkinsonHyperlegible_Bold", "AtkinsonHyperlegible-Bold.ttf"},
        {"font_caption", "caption.otf"},
        {"font_chirp_regular_web", "chirp-regular-web.woff2"},
        {"font_HelveticaNeue", "HelveticaNeue.otf"},
        {"font_ImpactMix", "ImpactMix.ttf"},
        {"font_TAHOMABD", "TAHOMABD.TTF"},
        {"font_times_new_roman", "times new roman.ttf"},
        {"font_TwemojiCOLR0", "TwemojiCOLR0.otf"},
        {"font_Ubuntu_R", "Ubuntu-R.ttf"},
        {"font_whisper", "whisper.otf"}
    }.AsReadOnly();


    public static bool IsAnimatedType(string contentType)
    {
        return new string[]
        {
            "image/gif",
        }.Contains(contentType);
    }
    public static string[] SupportedContentTypes =>
    [
        "image/png",
        "image/gif",
        "image/jpeg",
        "image/webp",
    ];

    public static async Task<MemoryStream> FetchData(IAttachment attachment)
    {
        var client = new HttpClient();
        var request = new HttpRequestMessage()
        {
            RequestUri = new Uri(attachment.Url),
            Method = HttpMethod.Get
        };
        var response = await client.SendAsync(request);
        var ms = new MemoryStream(4_194_304);
        await using var responseStream = await response.Content.ReadAsStreamAsync();
        await responseStream.CopyToAsync(ms);
        if (response.IsSuccessStatusCode)
        {
            return ms;
        }
        throw new HttpResponseException($"Request returned status {(int)response.StatusCode} for URL: {attachment.Url}")
        {
            Response = response,
            Request = request,
            ResponseStream = ms
        };
    }


    public static double[] WhiteRGBA => [255, 255, 255, 255];
    public static double[] WhiteRGB => [255, 255, 255];
}
