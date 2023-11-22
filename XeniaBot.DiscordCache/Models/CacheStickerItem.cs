using Discord;

namespace XeniaBot.DiscordCache.Models;

/// <summary>
/// Note: Unable to directly implement <see cref="IStickerItem"/> since the Id field clashes with MongoDB
/// </summary>
public class CacheStickerItem
{
    public ulong StickerId { get; set; }
    public string Name { get; set; }
    public StickerFormatType Format { get; set; }

    public CacheStickerItem Update(IStickerItem sticker)
    {
        StickerId = sticker.Id;
        Name = sticker.Name;
        Format = sticker.Format;
        return this;
    }

    public static CacheStickerItem? FromExisting(IStickerItem? item)
    {
        if (item == null)
            return null;

        var instance = new CacheStickerItem();
        return instance.Update(item);
    }
}