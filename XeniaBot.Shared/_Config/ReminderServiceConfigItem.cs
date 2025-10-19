using System.ComponentModel;

namespace XeniaBot.Shared;

public class ReminderServiceConfigItem
{
    [DefaultValue(false)]
    public bool Enable { get; set; } = false;

    public static ReminderServiceConfigItem Default(ReminderServiceConfigItem? i = null)
    {
        i ??= new ReminderServiceConfigItem();
        i.Enable = false;
        return i;
    }
    public ReminderServiceConfigItem()
    {
        Default(this);
    }
}