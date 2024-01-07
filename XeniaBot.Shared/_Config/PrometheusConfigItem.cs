namespace XeniaBot.Shared;

public class PrometheusConfigItem
{
    public bool Enable { get; set; }
    public int Port { get; set; }
    public string Url { get; set; }
    public string Hostname { get; set; }

    /// <summary>
    /// Reset to default values.
    /// </summary>
    public static PrometheusConfigItem Default(PrometheusConfigItem? i = null)
    {
        i ??= new PrometheusConfigItem();
        i.Enable = false;
        i.Port = 4828;
        i.Url = "/metrics";
        i.Hostname = "+";
        return i;
    }

    public PrometheusConfigItem()
    {
        Default(this);
    }
}