using Discord;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;

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