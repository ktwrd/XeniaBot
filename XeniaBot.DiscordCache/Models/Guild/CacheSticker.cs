using Discord;

namespace XeniaBot.DiscordCache.Models;

public class CacheSticker : CacheStickerItem
{
    public new ulong StickerId { get; set; }
    public ulong PackId { get; set; }
    public new string Name { get; set; }
    public string Description { get; set; }
    public string[] Tags { get; set; }
    public StickerType Type { get; set; }
    public new StickerFormatType Format { get; set; }
    public bool? IsAvailable { get; set; }
    public int? SortOrder { get; set; }
    public CacheSticker Update(ISticker sticker)
    {
        base.Update(sticker);
        StickerId = sticker.Id;
        PackId = sticker.PackId;
        Name = sticker.Name;
        Description = sticker.Description;
        Tags = sticker.Tags.ToArray();
        Type = sticker.Type;
        Format = sticker.Format;
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