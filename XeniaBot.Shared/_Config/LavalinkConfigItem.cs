namespace XeniaBot.Shared;

public class LavalinkConfigItem
{
    public bool Enable { get; set; }
    public string? Hostname { get; set; }
    public ushort Port { get; set; }
    public string Auth { get; set; }
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