using Discord;

namespace XeniaBot.DiscordCache.Models;

public class CacheMessageComponent : IMessageComponent
{
    public ComponentType Type { get; set; }
    public string CustomId { get; set; }

    public CacheMessageComponent()
    {
        CustomId = "";
    }

    public CacheMessageComponent Update(IMessageComponent component)
    {
        Type = component.Type;
        CustomId = component.CustomId;
        return this;
    }

    public static CacheMessageComponent? FromExisting(IMessageComponent? component)
    {
        if (component == null)
            return null;

        var instance = new CacheMessageComponent();
        return instance.Update(component);
    }
}