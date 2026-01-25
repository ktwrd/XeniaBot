namespace XeniaBot.Shared.Models;

public class XeniaHealthModel
{
    /// <summary>
    /// UTC Timestamp (seconds) for when this service was launched.
    /// </summary>
    public long StartTimestamp { get; set; }
    /// <summary>
    /// Current version of that service.
    /// </summary>
    public string Version { get; set; }
    /// <summary>
    /// Name of this service.
    /// </summary>
    public string ServiceName { get; set; }

    public XeniaHealthModel()
    {
        Version = "unknown";
        ServiceName = "unknown";
        StartTimestamp = 0;
    }
}