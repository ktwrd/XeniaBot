using System.ComponentModel;
using System.Xml.Serialization;

namespace XeniaBot.Shared.Config;

public class DashboardConfigElement
{
    [DefaultValue(false)]
    [XmlAttribute("Has")]
    public bool Has { get; set; }

    [XmlElement("Url")]
    public string? Url { get; set; }
}
