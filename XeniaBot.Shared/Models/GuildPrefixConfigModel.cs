using System.Xml.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace XeniaBot.Shared.Models;

public class GuildPrefixConfigModel : BaseModel
{
    public ulong GuildId { get; set; }
    public string? Prefix { get; set; }

    public GuildPrefixConfigModel()
    {
        GuildId = 0;
        Prefix = null;
    }

    [BsonIgnore]
    [JsonIgnore]
    [XmlIgnore]
    public FilterDefinition<GuildPrefixConfigModel> Filter =>
        Builders<GuildPrefixConfigModel>.Filter.Where(e => e.GuildId == GuildId);
    public GuildPrefixConfigModel(ulong guildId)
    {
        GuildId = guildId;
        Prefix = null;
    }
}