using System.Xml.Serialization;

namespace XeniaBot.Shared.Config;

public class ErrorReportConfigElement
{
    [XmlAttribute("Guild")]
    public ulong? GuildId { get; set; }
    [XmlAttribute("Channel")]
    public ulong? ChannelId { get; set; }
}
