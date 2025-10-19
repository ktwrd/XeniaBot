using System.Xml.Serialization;

namespace XeniaBot.Shared.Config;

public class BanSyncConfigElement
{
    [XmlElement("GuildId")]
    public ulong? GuildId { get; set; }

    [XmlElement("GuildStateChangedChannelId")]
    public ulong? GuildStateChangedChannelId { get; set; }

    [XmlElement("FeatureRequestChannelId")]
    public ulong? FeatureRequestChannelId { get; set; }
}
