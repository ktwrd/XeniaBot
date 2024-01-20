using XeniaBot.DiscordCache.Models;
using XeniaBot.Shared.Models;

namespace XeniaBot.Data.Models;

public class AttachmentArchiveModel : BaseModel
{
    public static string CollectionName => "attachmentArchive";
    public string ArchiveId { get; set; }
    public ulong MessageId { get; set; }
    public CacheMessageAttachment AttachmentDetails { get; set; }
    public string BucketLocation { get; set; }

    public AttachmentArchiveModel()
    {
        ArchiveId = Guid.NewGuid().ToString();
    }
}