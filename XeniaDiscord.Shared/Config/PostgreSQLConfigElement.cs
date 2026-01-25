using System.ComponentModel;
using System.Xml.Serialization;

namespace XeniaBot.Shared.Config;

public class PostgreSQLConfigElement
{
    [DefaultValue("postgres")]
    [XmlAttribute("Host")]
    public string Host { get; set; } = "postgres";

    [DefaultValue(5432)]
    [XmlAttribute("Port")]
    public int Port { get; set; } = 5432;

    [DefaultValue("xenia")]
    [XmlAttribute("Name")]
    public string Name { get; set; } = "xenia";

    [DefaultValue("postgres")]
    [XmlElement("Username")]
    public string Username { get; set; } = "postgres";

    [DefaultValue("")]
    [XmlElement("Password")]
    public string Password { get; set; } = "";
}
