using System;
using System.Text.Json;
using Discord;

namespace XeniaBot.Core.Controllers.Wrappers.BigBrother;


/// <summary>
/// This can be deserialized from <see cref="IEmbed"/> with <see cref="BB_MessageEmbed.FromEmbed()"/>
/// </summary>
public class BB_MessageEmbed
{
    public string Url { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public EmbedType Type { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public Discord.Color? Color { get; set; }
    public EmbedImage? Image { get; set; }
    public EmbedVideo? Video { get; set; }
    public BB_MessageEmbedAuthor? Author { get; set; }
    public BB_MessageEmbedFooter? Footer { get; set; }
    public BB_MessageEmbedProvider? Provider { get; set; }
    public BB_MessageEmbedThumbnail? Thumbnail { get; set; }
    public BB_MessageEmbedField[] Fields { get; set; }

    public BB_MessageEmbed()
    {
        Url = "";
        Title = "";
        Description = "";
        Fields = Array.Empty<BB_MessageEmbedField>();
    }
    public static BB_MessageEmbed? FromEmbed(IEmbed embed)
    {
        var opts = new JsonSerializerOptions()
        {
            IgnoreReadOnlyFields = true,
            IgnoreReadOnlyProperties = true,
            IncludeFields = true
        };
        var text = JsonSerializer.Serialize(embed, opts);
        return JsonSerializer.Deserialize<BB_MessageEmbed>(text, opts);
    }
}
#region Message Embed Fields
public class BB_MessageEmbedField
{
    public string Name { get; set; }
    public string Value { get; set; }
    public bool Inline { get; set; }

    public BB_MessageEmbedField()
    {
        Name = "";
        Value = "";
        Inline = false;
    }

    public bool Equals(BB_MessageEmbedField? embedField)
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
public class BB_MessageEmbedThumbnail
{
    public string Url { get; set; }
    public string ProxyUrl { get; set; }
    public int? Height { get; set; }
    public int? Width { get; set; }

    public BB_MessageEmbedThumbnail()
    {
        Url = "";
        ProxyUrl = "";
    }
}
public class BB_MessageEmbedProvider
{
    public string Name { get; set; }
    public string Url { get; set; }

    public BB_MessageEmbedProvider()
    {
        Name = "";
        Url = "";
    }
}
public class BB_MessageEmbedFooter
{
    public string Text { get; set; }    
    public string IconUrl { get; set; }
    public string ProxyUrl { get; set; }

    public BB_MessageEmbedFooter()
    {
        Text = "";
        IconUrl = "";
        ProxyUrl = "";
    }
}
public class BB_MessageEmbedAuthor
{
    public string Name { get; set; }
    public string Url { get; set; }
    public string IconUrl { get; set; }
    public string ProxyIconUrl { get; set; }

    public BB_MessageEmbedAuthor()
    {
        Name = "";
        Url = "";
        IconUrl = "";
        ProxyIconUrl = "";
    }
}
#endregion