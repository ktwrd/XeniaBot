namespace XeniaBot.Shared;

public class EvidenceServiceConfig
{
    public bool Enable { get; set; }
    public GoogleCloudKey? GoogleCloudKey { get; set; }
    /// <summary>
    /// Maximum upload size for attachments (bytes)
    /// </summary>
    public int MaximumUploadSize { get; set; }

    public static EvidenceServiceConfig Default(EvidenceServiceConfig? i = null)
    {
        i ??= new EvidenceServiceConfig();
        i.Enable = false;
        i.GoogleCloudKey = null;
        i.MaximumUploadSize = 8000000; // 8mb
        return i;
    }

    public EvidenceServiceConfig()
    {
        Default(this);
    }
}