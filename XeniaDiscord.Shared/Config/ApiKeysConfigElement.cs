using System.Xml.Serialization;

namespace XeniaBot.Shared.Config;

public class ApiKeysConfigElement
{
    [XmlElement("Weather")]
    public string? Weather { get; set; }

    [XmlElement("GoogleCloud.Translate.Base64")]
    public string? GoogleCloudTranslateAsBase64 { get; set; }
}
