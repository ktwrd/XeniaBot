using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace XeniaBot.Shared.Config;

public class DiscordConfigElement
{
    [Required]
    [XmlElement("Token")]
    public string Token { get; set; } = "";

    [Required]
    [XmlElement("PublicKey")]
    public string PublicKey { get; set; } = "";

    [DefaultValue(null)]
    [XmlElement("ShardId")]
    public int? ShardId { get; set; }

    [XmlElement("SuperuserId")]
    public List<ulong> SuperuserIds { get; set; } = [];

    [XmlElement("BanSync")]
    public BanSyncConfigElement BanSync { get; set; } = new();

    [XmlElement("InvitePermissions")]
    public ulong InvitePermissions { get; set; }
}
