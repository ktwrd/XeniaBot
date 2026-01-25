using System.ComponentModel;
using System.Xml.Serialization;

namespace XeniaBot.Shared.Config;

public class ReminderServiceConfigElement
{
    [DefaultValue(false)]
    [XmlAttribute("Enabled")]
    public bool Enabled { get; set; } = false;
}
