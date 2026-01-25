using System.ComponentModel;

namespace XeniaBot.Shared;

public class ESixConfigItem
{
    [DefaultValue(false)]
    public bool Enable { get; set; }
    [DefaultValue("")]
    public string Username { get; set; }
    [DefaultValue("")]
    public string ApiKey { get; set; }

    public static ESixConfigItem Default(ESixConfigItem? i = null)
    {
        i ??= new ESixConfigItem();
        i.Enable = false;
        i.Username = "";
        i.ApiKey = "";
        return i;
    }

    public ESixConfigItem()
    {
        Default(this);
    }
}