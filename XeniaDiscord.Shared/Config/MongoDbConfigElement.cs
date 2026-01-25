using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace XeniaBot.Shared.Config;

public class MongoDbConfigElement
{
    [Required]
    [XmlAttribute("ConnectionUrl")]
    public string ConnectionUrl { get; set; } = "";

    [Required]
    [XmlAttribute("Database")]
    public string Database { get; set; } = "";
}
