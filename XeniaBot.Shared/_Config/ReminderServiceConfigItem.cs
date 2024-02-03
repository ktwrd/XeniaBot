namespace XeniaBot.Shared;

public class ReminderServiceConfigItem
{
    public bool Enable { get; set; }

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