using Discord;

namespace XeniaBot.DiscordCache.Models;

public class CacheStickerItem : IStickerItem
{
    public ulong Id { get; set; }
    public string Name { get; set; }
    public StickerFormatType Format { get; set; }

    public CacheStickerItem FromExisting(IStickerItem sticker)
    {
        Id = sticker.Id;
        Name = sticker.Name;
        Format = sticker.Format;
        return this;
    }
}