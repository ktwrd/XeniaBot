using System.Text.Json;
using Discord;

namespace XeniaBot.DiscordCache.Models;

public class CacheMessageEmbed
{
    public string Url { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public EmbedType Type { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public Discord.Color? Color { get; set; }
    public EmbedImage? Image { get; set; }
    public EmbedVideo? Video { get; set; }
    public CacheMessageEmbedAuthor? Author { get; set; }
    public CacheMessageEmbedFooter? Footer { get; set; }
    public CacheMessageEmbedProvider? Provider { get; set; }
    public CacheMessageEmbedThumbnail? Thumbnail { get; set; }
    public CacheMessageEmbedField[] Fields { get; set; }

    public CacheMessageEmbed()
    {
        Url = "";
        Title = "";
        Description = "";
        Fields = Array.Empty<CacheMessageEmbedField>();
    }
    public static CacheMessageEmbed? FromEmbed(IEmbed embed)
    {
        var opts = new JsonSerializerOptions()
        {
            IgnoreReadOnlyFields = true,
            IgnoreReadOnlyProperties = true,
            IncludeFields = true
        };
        var text = JsonSerializer.Serialize(embed, opts);
        return JsonSerializer.Deserialize<CacheMessageEmbed>(text, opts);
    }
}