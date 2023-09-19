using Discord;

namespace XeniaBot.DiscordCache.Models;

public class CacheSticker : CacheStickerItem
{
    public ulong Id { get; set; }
    public ulong PackId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string[] Tags { get; set; }
    public StickerType Type { get; set; }
    public StickerFormatType Format { get; set; }
    public bool? IsAvailable { get; set; }
    public int? SortOrder { get; set; }

    public CacheSticker FromExisting(ISticker sticker)
    {
        base.FromExisting(sticker);
        Id = sticker.Id;
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
}