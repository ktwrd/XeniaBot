namespace XeniaBot.Shared;

public class AttachmentArchiveConfigItem
{
    public bool Enable { get; set; }
    public string? BucketName { get; set; }
    public string BasePath { get; set; }

    public static AttachmentArchiveConfigItem Default(AttachmentArchiveConfigItem? i = null)
    {
        i ??= new AttachmentArchiveConfigItem();
        i.Enable = false;
        i.BucketName = null;
        i.BasePath = null;
        return i;
    }

    public AttachmentArchiveConfigItem()
    {
        Default(this);
    }
}