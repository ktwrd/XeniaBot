namespace XeniaBot.Evidence.Models;

/// <summary>
/// Used when providing details to end-users.
/// </summary>
public class EvidenceFileModelDetails
{
    /// <summary>
    /// Id of <see cref="EvidenceFileModel"/>
    /// </summary>
    public string Id { get; set; }
    /// <summary>
    /// <see cref="EvidenceFileModel.Filename"/>
    /// </summary>
    public string Filename { get; set; }
    /// <summary>
    /// <see cref="EvidenceFileModel.Description"/>
    /// </summary>
    public string Description { get; set; }
    /// <summary>
    /// <see cref="EvidenceFileModel.ContentType"/>
    /// </summary>
    public string ContentType { get; set; }
    /// <summary>
    /// <see cref="EvidenceFileModel.GetSize()"/>
    /// </summary>
    public ulong Size { get; set; }
    /// <summary>
    /// <see cref="EvidenceFileModel.GetUploadedByUserId()"/>
    /// </summary>
    public ulong UserId { get; set; }
    /// <summary>
    /// <see cref="EvidenceFileModel.GetGuildId()"/>
    /// </summary>
    public ulong GuildId { get; set; }
    /// <summary>
    /// <see cref="EvidenceFileModel.UploadedAtTimestamp"/>
    /// </summary>
    public long CreatedAt { get; set; }

    public EvidenceFileModelDetails()
    {
        Id = "";
        Filename = "";
        Description = "";
        ContentType = "application/octet-stream";
    }
}