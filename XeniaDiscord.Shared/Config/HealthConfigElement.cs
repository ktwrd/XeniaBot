using System.ComponentModel;
using System.Xml.Serialization;

namespace XeniaDiscord.Shared.Config;

public class HealthConfigElement
{
    [DefaultValue(false)]
    [XmlAttribute("Enable")]
    public bool Enabled { get; set; }

    [DefaultValue(false)]
    [XmlAttribute("Port")]
    public ushort Port { get; set; }

    /// <summary>
    /// <b>OPTIONAL:</b> IP Address to listen on. Must be parseable by: <see cref="System.Net.IPAddress"/>
    /// </summary>
    [XmlElement("Address")]
    public string? Address { get; set; }

    [XmlElement("Ssl")]
    public GenHttpSslConfigElement SslConfig { get; set; } = new();
}
