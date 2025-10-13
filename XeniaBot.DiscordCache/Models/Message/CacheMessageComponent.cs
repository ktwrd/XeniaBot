using System.Text.Json.Serialization;
using Discord;
using MongoDB.Bson.Serialization.Attributes;

namespace XeniaBot.DiscordCache.Models;

public class CacheMessageComponent : IMessageComponent
{
    public ComponentType Type { get; set; }
    public string CustomId { get; set; }
    [BsonIgnore]
    [JsonIgnore]
    public int? Id
    {
        get
        {
            if (int.TryParse(CustomId, out var r)) return r;
            return null;
        }
    }

    public CacheMessageComponent()
    {
        CustomId = "";
    }

    public CacheMessageComponent Update(IMessageComponent component)
    {
        Type = component.Type;
        CustomId = component.Id?.ToString() ?? "";
        return this;
    }

    public static CacheMessageComponent? FromExisting(IMessageComponent? component)
    {
        if (component == null)
            return null;

        var instance = new CacheMessageComponent();
        return instance.Update(component);
    }
    
    public IMessageComponentBuilder? ToBuilder() => null;
}