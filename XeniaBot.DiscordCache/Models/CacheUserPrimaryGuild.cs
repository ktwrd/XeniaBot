using System.Reflection;
using Discord;
using MongoDB.Bson.Serialization.Attributes;

namespace XeniaBot.DiscordCache.Models;

public class CacheUserPrimaryGuild
{
    public CacheUserPrimaryGuild()
    {
        Tag = "";
        BadgeHash = "";
    }
    public CacheUserPrimaryGuild(PrimaryGuild other)
    {
        GuildId = other.GuildId?.ToString();
        IdentityEnabled = other.IdentityEnabled;
        Tag = string.IsNullOrEmpty(other.Tag?.Trim()) ? string.Empty : other.Tag;
        BadgeHash = string.IsNullOrEmpty(other.BadgeHash?.Trim()) ? string.Empty : other.BadgeHash;
    }
    
    [BsonIgnoreIfNull]
    public string? GuildId { get; set; }
    public ulong? GetGuildId()
    {
        if (ulong.TryParse(GuildId, out var result)) return result;
        return null;
    }
    [BsonIgnoreIfNull]
    public bool? IdentityEnabled { get; set; }
    public string Tag { get; set; }
    public string BadgeHash { get; set; }
    
    public void Update(CacheUserPrimaryGuild? other)
    {
        if (other == null) return;
        
        GuildId = other.GuildId;
        IdentityEnabled = other.IdentityEnabled;
        Tag = other.Tag;
        BadgeHash = other.BadgeHash;
    }
    public void Update(PrimaryGuild? other)
    {
        if (other == null) return;
        
        GuildId = other.Value.GuildId?.ToString();
        IdentityEnabled = other.Value.IdentityEnabled;
        Tag = string.IsNullOrEmpty(other.Value.Tag?.Trim()) ? string.Empty : other.Value.Tag;
        BadgeHash = string.IsNullOrEmpty(other.Value.BadgeHash?.Trim()) ? string.Empty : other.Value.BadgeHash;
    }
    
    public PrimaryGuild ToPrimaryGuild()
    {
        var ctor = typeof(PrimaryGuild).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .FirstOrDefault(e =>
            {
                var p = e.GetParameters();
                return p.Length == 4
                    && p[0].ParameterType == typeof(ulong?)
                    && p[1].ParameterType == typeof(bool?)
                    && p[2].ParameterType == typeof(string)
                    && p[3].ParameterType == typeof(string);
            });
        var r = ctor?.Invoke(new object?[]
        {
            GetGuildId(),
            IdentityEnabled,
            Tag,
            BadgeHash
        });
        if (r is PrimaryGuild guild) return guild;
        throw new InvalidOperationException($"Could not convert this instance to type {typeof(PrimaryGuild)} (ctor type: {ctor})");
    }
}