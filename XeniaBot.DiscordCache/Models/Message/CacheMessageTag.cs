using Discord;
using System.Text.Json;
using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace XeniaBot.DiscordCache.Models;

public class CacheMessageTag : ITag
{
    public int Index { get; set; }
    public int Length { get; set; }
    public TagType Type { get; set; }
    public ulong Key { get; set; }
    
    public ICacheMessageTagValue? Value { get; set; }
    object? ITag.Value => this.Value;

    public string? ValueJson { get; set; }

    public CacheMessageTag Update(ITag tag)
    {
        Index = tag.Index;
        Length = tag.Length;
        Type = tag.Type;
        Key = tag.Key;
        if (tag.Value != null)
        {
            try
            {
                ValueJson = JsonSerializer.Serialize(tag.Value, SerializerOptions);
            }
            catch { }
        }

        Value = null;
        
        if (tag.Value is IUser user)
            Value = CacheMessageTagUserValue.FromExisting(user);
        else if (tag.Value is Emote discEmote)
            Value = CacheMessageTagEmoteValue.FromExisting(discEmote);
        else if (tag.Value is IRole role)
            Value = CacheMessageTagRoleValue.FromExisting(role);
        // TODO implement value for TagType.ChannelMention
        
        return this;
    }

    private static JsonSerializerOptions SerializerOptions => new()
    {
        WriteIndented = true,
        IncludeFields = true,
        ReferenceHandler = ReferenceHandler.Preserve,
        MaxDepth = 12,
    };

    public static CacheMessageTag? FromExisting(ITag? tag)
    {
        if (tag == null)
            return null;

        var instance = new CacheMessageTag();
        return instance.Update(tag);
    }
}

public interface ICacheMessageTagValue
{
}

[BsonDiscriminator("CacheEmote")]
public class CacheMessageTagEmoteValue : CacheEmote, ICacheMessageTagValue
{
    public static CacheMessageTagEmoteValue? FromExisting(Emote? emote)
    {
        if (emote == null) return null;
        var instance = new CacheMessageTagEmoteValue();
        instance.Update(emote);
        return instance;
    }
}

[BsonDiscriminator("CacheUserModel")]
public class CacheMessageTagUserValue : CacheUserModel, ICacheMessageTagValue
{
    public new static CacheMessageTagUserValue? FromExisting(IUser? user)
    {
        if (user == null) return null;
        var instance = new CacheMessageTagUserValue();
        instance.Update(user);
        return instance;
    }
}

[BsonDiscriminator("CacheRole")]
public class CacheMessageTagRoleValue : CacheRole, ICacheMessageTagValue
{
    public new static CacheMessageTagRoleValue? FromExisting(IRole? value)
    {
        if (value == null) return null;
        var instance = new CacheMessageTagRoleValue();
        instance.Update(value);
        return instance;
    }
}