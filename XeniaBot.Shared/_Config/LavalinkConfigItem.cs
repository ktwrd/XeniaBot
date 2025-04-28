using System.ComponentModel;

namespace XeniaBot.Shared;

public class LavalinkConfigItem
{
    [DefaultValue(false)]
    public bool Enable { get; set; }
    [DefaultValue(null)]
    public string? Hostname { get; set; }
    [DefaultValue(2333)]
    public ushort Port { get; set; }
    [DefaultValue("")]
    public string Auth { get; set; }
    [DefaultValue(false)]
    public bool Secure { get; set; }

    public static LavalinkConfigItem Default(LavalinkConfigItem i)
    {
        i.Enable = false;
        i.Hostname = null;
        i.Port = 2333;
        i.Auth = "";
        i.Secure = false;
        return i;
    }

    public LavalinkConfigItem()
    {
        Default(this);
    }
}