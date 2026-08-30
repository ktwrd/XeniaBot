using Discord;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace XeniaBot.DiscordCache.Models;

public class CacheMessageTag : ITag
{
    public int Index { get; set; }
    public int Length { get; set; }
    public TagType Type { get; set; }
    public ulong Key { get; set; }
    
    public BaseCacheMessageTag? Value { get; set; }
    
    [BsonIgnore]
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
[BsonDiscriminator(RootClass = true)]
[BsonKnownTypes(
    typeof(CacheMessageTagEmoteValue),
    typeof(CacheMessageTagUserValue),
    typeof(CacheMessageTagRoleValue))]
public class BaseCacheMessageTag
{
}

[BsonDiscriminator("CacheEmote")]
public class CacheMessageTagEmoteValue : BaseCacheMessageTag, ICacheEmote
{
    [BsonIgnoreIfNull]
    public string? Name { get; set; }

    /// <summary>
    /// ulong stored as string
    /// </summary>
    [BsonIgnoreIfNull]
    public string? Id { get; set; }

    [BsonIgnoreIfNull]
    public bool? Animated { get; set; }

    [BsonExtraElements()]
    [JsonIgnore]
    public IDictionary<string, object?> ExtraElements
    {
        get => field;
        set
        {
            field = value;
        }
    } = new Dictionary<string, object?>();

    public void Update(IEmote emote)
    {
        this.UpdateValues(emote);
    }

    public static CacheMessageTagEmoteValue? FromExisting(Emote? emote)
    {
        if (emote == null) return null;
        var instance = new CacheMessageTagEmoteValue();
        instance.Update(emote);
        return instance;
    }
}

[BsonDiscriminator("CacheUserModel")]
public class CacheMessageTagUserValue : BaseCacheMessageTag, ICacheUserModel
{
    #region IDiscordCacheBaseModel
    public ulong Snowflake { get; set; }
    public long ModifiedAtTimestamp { get; set; }
    #endregion

    #region ICacheUserModel
    #region ISnowflakeEntity
    public DateTimeOffset CreatedAt { get; set; }
    #endregion

    #region IUser
    public string AvatarId { get; set; } = string.Empty;
    public string Discriminator { get; set; } = string.Empty;
    public ushort DiscriminatorValue { get; set; }

    public bool IsBot { get; set; }
    public bool IsWebhook { get; set; }
    public string Username { get; set; } = string.Empty;
    public string GlobalName { get; set; } = string.Empty;
    public string AvatarDecorationHash { get; set; } = string.Empty;
    [BsonIgnoreIfNull]
    public ulong? AvatarDecorationSkuId { get; set; }
    [BsonIgnoreIfNull]
    public UserProperties? PublicFlags { get; set; }
    [BsonIgnoreIfNull]
    public CacheUserPrimaryGuild? PrimaryGuild { get; set; }

    #region IMentionable
    public string Mention { get; set; } = string.Empty;
    #endregion

    #region IPresence
    public UserStatus Status { get; set; }
    public ClientType[] ActiveClients { get; set; }
    public CacheUserActivity[] Activities { get; set; }
    #endregion
    #endregion
    #endregion

    [BsonExtraElements()]
    [JsonIgnore]
    public IDictionary<string, object?> ExtraElements
    {
        get => field;
        set
        {
            field = value;
        }
    } = new Dictionary<string, object?>();

    public CacheMessageTagUserValue()
    {
        ActiveClients = Array.Empty<ClientType>();
        Activities = Array.Empty<CacheUserActivity>();
    }

    public virtual void Update(IUser user)
    {
        this.UpdateValues(user);
    }

    public new static CacheMessageTagUserValue? FromExisting(IUser? user)
    {
        if (user == null) return null;
        var instance = new CacheMessageTagUserValue();
        instance.Update(user);
        return instance;
    }
}

[BsonDiscriminator("CacheRole")]
public class CacheMessageTagRoleValue : BaseCacheMessageTag, ICacheRole
{
    #region ICacheRole
    public ulong? RoleId { get; set; }
    public ulong GuildId { get; set; }
    public string Color { get; set; } = string.Empty;
    public bool IsHoisted { get; set; }
    public bool IsManaged { get; set; }
    public bool IsMentionable { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public CacheEmote? Emoji { get; set; }
    public CacheGuildPermissions Permissions { get; set; } = CacheGuildPermissions.None;
    public int Position { get; set; }
    public RoleTags? Tags { get; set; }
    #endregion

    [BsonExtraElements()]
    [JsonIgnore]
    public IDictionary<string, object?> ExtraElements
    {
        get => field;
        set
        {
            field = value;
        }
    } = new Dictionary<string, object?>();

    public CacheMessageTagRoleValue()
    {
        this.UseDefaultValues();
    }

    public void Update(IRole role)
    {
        this.UpdateValues(role);
    }

    public new static CacheMessageTagRoleValue? FromExisting(IRole? value)
    {
        if (value == null) return null;
        var instance = new CacheMessageTagRoleValue();
        instance.Update(value);
        return instance;
    }
}