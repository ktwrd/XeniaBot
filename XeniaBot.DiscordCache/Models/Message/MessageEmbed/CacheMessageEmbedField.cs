using Discord;

namespace XeniaBot.DiscordCache.Models;

public class CacheMessageEmbedField
{
    public string Name { get; set; }
    public string Value { get; set; }
    public bool Inline { get; set; }

    public CacheMessageEmbedField()
    {
        Name = "";
        Value = "";
        Inline = false;
    }

    public bool Equals(CacheMessageEmbedField? embedField)
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