using System;
using System.Text.Json;
using Discord;

namespace XeniaBot.Core.Controllers.Wrappers.Archival;


/// <summary>
/// This can be deserialized from <see cref="IEmbed"/> with <see cref="XMessageEmbed.FromEmbed()"/>
/// </summary>
public class XMessageEmbed
{
    public string Url { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public EmbedType Type { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public Discord.Color? Color { get; set; }
    public EmbedImage? Image { get; set; }
    public EmbedVideo? Video { get; set; }
    public XMessageEmbedAuthor? Author { get; set; }
    public XMessageEmbedFooter? Footer { get; set; }
    public XMessageEmbedProvider? Provider { get; set; }
    public XMessageEmbedThumbnail? Thumbnail { get; set; }
    public XMessageEmbedField[] Fields { get; set; }

    public XMessageEmbed()
    {
        Url = "";
        Title = "";
        Description = "";
        Fields = Array.Empty<XMessageEmbedField>();
    }
    public static XMessageEmbed? FromEmbed(IEmbed embed)
    {
        var opts = new JsonSerializerOptions()
        {
            IgnoreReadOnlyFields = true,
            IgnoreReadOnlyProperties = true,
            IncludeFields = true
        };
        var text = JsonSerializer.Serialize(embed, opts);
        return JsonSerializer.Deserialize<XMessageEmbed>(text, opts);
    }
}
#region Message Embed Fields
public class XMessageEmbedField
{
    public string Name { get; set; }
    public string Value { get; set; }
    public bool Inline { get; set; }

    public XMessageEmbedField()
    {
        Name = "";
        Value = "";
        Inline = false;
    }

    public bool Equals(XMessageEmbedField? embedField)
    {
        int hashCode1 = this.GetHashCode();
        int? hashCode2 = embedField?.GetHashCode();
        int valueOrDefault = hashCode2.GetValueOrDefault();
        return hashCode1 == valueOrDefault & hashCode2.HasValue;
    }

    public bool Equals(EmbedField? embedField)
    {
        int hashCode1 = this.GetHashCode();
        int? hashCode2 = embedField?.GetHashCode();
        int valueOrDefault = hashCode2.GetValueOrDefault();
        return hashCode1 == valueOrDefault & hashCode2.HasValue;
    }
}
public class XMessageEmbedThumbnail
{
    public string Url { get; set; }
    public string ProxyUrl { get; set; }
    public int? Height { get; set; }
    public int? Width { get; set; }

    public XMessageEmbedThumbnail()
    {
        Url = "";
        ProxyUrl = "";
    }
}
public class XMessageEmbedProvider
{
    public string Name { get; set; }
    public string Url { get; set; }

    public XMessageEmbedProvider()
    {
        Name = "";
        Url = "";
    }
}
public class XMessageEmbedFooter
{
    public string Text { get; set; }    
    public string IconUrl { get; set; }
    public string ProxyUrl { get; set; }

    public XMessageEmbedFooter()
    {
        Text = "";
        IconUrl = "";
        ProxyUrl = "";
    }
}
public class XMessageEmbedAuthor
{
    public string Name { get; set; }
    public string Url { get; set; }
    public string IconUrl { get; set; }
    public string ProxyIconUrl { get; set; }

    public XMessageEmbedAuthor()
    {
        Name = "";
        Url = "";
        IconUrl = "";
        ProxyIconUrl = "";
    }
}
#endregion