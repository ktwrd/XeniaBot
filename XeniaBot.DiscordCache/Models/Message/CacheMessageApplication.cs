using Discord;

namespace XeniaBot.DiscordCache.Models;

public class CacheMessageApplication
{
    public ulong Id { get; set; }
    public string? CoverImage { get; set; }
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string? IconUrl { get; set; }
    public string Name { get; set; }

    public CacheMessageApplication Update(MessageApplication app)
    {
        Id = app.Id;
        CoverImage = app.CoverImage;
        Description = app.Description;
        Icon = app.Icon;
        IconUrl = app.IconUrl;
        Name = app.Name;
        return this;
    }

    public static CacheMessageApplication? FromExisting(MessageApplication? app)
    {
        if (app == null)
            return null;

        var instance = new CacheMessageApplication();
        return instance.Update(app);
    }
}