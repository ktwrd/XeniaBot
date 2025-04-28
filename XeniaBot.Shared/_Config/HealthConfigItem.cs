using System.ComponentModel;

namespace XeniaBot.Shared;

public class HealthConfigItem
{
    [DefaultValue(false)]
    public bool Enable { get; set; }
    [DefaultValue(4829)]
    public int Port { get; set; }

    public static HealthConfigItem Default(HealthConfigItem? i = null)
    {
        i ??= new HealthConfigItem();
        i.Enable = false;
        i.Port = 4829;
        return i;
    }

    public HealthConfigItem()
    {
        Default(this);
    }
}