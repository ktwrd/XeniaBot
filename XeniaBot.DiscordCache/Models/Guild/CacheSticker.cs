using Discord;
using MongoDB.Bson.Serialization.Attributes;

namespace XeniaBot.DiscordCache.Models;

public class CacheSticker : CacheStickerItem
{
    public ulong PackId { get; set; }
    public string Description { get; set; } = "";
    public string[] Tags { get; set; } = [];
    public StickerType Type { get; set; }
    [BsonIgnoreIfNull]
    public bool? IsAvailable { get; set; }
    [BsonIgnoreIfNull]
    public int? SortOrder { get; set; }
    public CacheSticker Update(ISticker sticker)
    {
        base.Update(sticker);
        PackId = sticker.PackId;
        Description = sticker.Description;
        Tags = sticker.Tags.ToArray();
        Type = sticker.Type;
        IsAvailable = sticker.IsAvailable;
        SortOrder = sticker.SortOrder;
        return this;
    }
    public static CacheSticker? FromExisting(ISticker? sticker)
    {
        if (sticker == null)
            return null;

        var instance = new CacheSticker();
        return instance.Update(sticker);
    }
}