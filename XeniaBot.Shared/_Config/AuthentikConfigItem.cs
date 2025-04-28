using System.ComponentModel;

namespace XeniaBot.Shared;

public class AuthentikConfigItem
{
    [DefaultValue(false)]
    public bool Enable { get; set; } = false;
    [DefaultValue("")]
    public string Token { get; set; } = "";
    [DefaultValue("")]
    public string Url { get; set; } = "";

    public static AuthentikConfigItem Default(AuthentikConfigItem? i = null)
    {
        i ??= new AuthentikConfigItem();
        i.Enable = false;
        i.Token = "";
        i.Url = "";
        return i;
    }

    public AuthentikConfigItem()
    {
        Default(this);
    }
}