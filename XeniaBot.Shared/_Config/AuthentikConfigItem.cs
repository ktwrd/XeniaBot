namespace XeniaBot.Shared;

public class AuthentikConfigItem
{
    public bool Enable { get; set; }
    public string Token { get; set; }
    public string Url { get; set; }

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