using Discord;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using Newtonsoft.Json;

namespace XeniaBot.DiscordCache.Models;

public class CacheMessageReference
{
    [BsonIgnoreIfNull]
    public ulong? MessageId { get; set; }
    [BsonIgnoreIfNull]
    public ulong? ChannelId { get; set; }
    [BsonIgnoreIfNull]
    public ulong? GuildId { get; set; }
    [BsonIgnoreIfNull]
    public bool? FailIfNotExists { get; set; }
    [BsonIgnoreIfNull]
    public MessageReferenceType? ReferenceType { get; set; }
    
    private IDictionary<string, object> _extraElements = new Dictionary<string, object>();
    [BsonExtraElements()]
    [JsonIgnore]
    public IDictionary<string, object> ExtraElements
    {
        get => _extraElements;
        set
        {
            bool p = false;
            if (value.TryGetValue("ChannelID", out var o ))
            {
                if (o is ulong?)
                {
                    ChannelId = (ulong?)o;
                    p = true;
                }
            }
            _extraElements = value;
            if (p)
            {
                _extraElements.Remove("ChannelID");
            }
        }
    }
    
    public CacheMessageReference Update(MessageReference r)
    {
        var a = r.MessageId.ToNullable();
        MessageId = a.HasValue ? a.Value : null;

        ChannelId = r.ChannelId;

        var b = r.GuildId.ToNullable();
        GuildId = b.HasValue ? b.Value : null;

        var c = r.FailIfNotExists.ToNullable();
        FailIfNotExists = c.HasValue ? c.Value : null;

        var d = r.ReferenceType.ToNullable();
        ReferenceType = d.HasValue ? d.Value : null;

        return this;
    }
    public static CacheMessageReference? FromExisting(MessageReference? r)
    {
        if (r == null)
            return null;

        var instance = new CacheMessageReference();
        return instance.Update(r);
    }
}