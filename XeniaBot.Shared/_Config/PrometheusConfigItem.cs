using System.ComponentModel;

namespace XeniaBot.Shared;

public class PrometheusConfigItem
{
    [DefaultValue(false)]
    public bool Enable { get; set; } = false;
    [DefaultValue(4828)]
    public int Port { get; set; } = 4828;
    [DefaultValue("/metrics")]
    public string Url { get; set; } = "/metrics";
    [DefaultValue("+")]
    public string Hostname { get; set; } = "+";

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