namespace XeniaBot.Shared;

public class ESixConfigItem
{
    public bool Enable { get; set; }
    public string Username { get; set; }
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