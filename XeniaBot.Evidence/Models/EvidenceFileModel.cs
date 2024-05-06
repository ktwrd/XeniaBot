using XeniaBot.Shared.Models;

namespace XeniaBot.Evidence.Models;

public class EvidenceFileModel : BaseModelGuid
{
    public static string CollectionName => "evidence_file";
    
    /// <summary>
    /// Filename of the file that was uploaded.
    /// </summary>
    public string Filename { get; set; }
    
    /// <summary>
    /// Description for this file.
    /// </summary>
    public string Description { get; set; }
    
    /// <summary>
    /// <para>Content Type of the file reported by Discord or Web Panel.</para>
    ///
    /// <para>Defaults to application/octet-stream</para>
    /// </summary>
    public string ContentType { get; set; }
    
    /// <summary>
    /// <para><b>Stored as ulong</b></para>
    ///
    /// <inheritdoc cref="GetSize()"/>
    /// </summary>
    public string Size { get; set; }

    /// <summary>
    /// File size. Measured in bytes.
    /// </summary>
    public ulong GetSize()
    {
        return ulong.Parse(Size);
    }
    
    /// <summary>
    /// <para><b>Stored as ulong</b></para>
    ///
    /// <inheritdoc cref="GetUploadedByUserId"/>
    /// </summary>
    public string UploadedByUserId { get; set; }
    
    /// <summary>
    /// <para>User Id for the User that Uploaded this file.</para>
    /// </summary>
    public ulong GetUploadedByUserId()
    {
        return ulong.Parse(UploadedByUserId);
    }
    
    /// <summary>
    /// <para><b>Stored as ulong</b></para>
    /// <inheritdoc cref="GetGuildId()"/>
    /// </summary>
    public string GuildId { get; set; }
    
    /// <summary>
    /// <para>Guild Id this File was uploaded from.</para>
    /// </summary>
    public ulong GetGuildId()
    {
        return ulong.Parse(GuildId);
    }
    
    /// <summary>
    /// Location to this file in the GCS Bucket.
    /// </summary>
    public string ObjectLocation { get; set; }
    
    /// <summary>
    /// Unix Timestamp (UTC, <b>Seconds</b>)
    /// </summary>
    public long UploadedAtTimestamp { get; set; }
    
    /// <summary>
    /// Is this file deleted from the Storage Bucket?
    /// </summary>
    public bool IsDeleted { get; set; }
    
    /// <summary>
    /// Is this file marked for deletion?
    /// </summary>
    public bool IsMarkedForDeletion { get; set; }
    
    /// <summary>
    /// <para>Unix Timestamp (UTC, <b>Seconds</b>)</para>
    ///
    /// <para>When this file was marked for deletion.</para>
    /// </summary>
    public long? MarkedForDeletedAt { get; set; }
    
    /// <summary>
    /// <para>Unix Timestamp (UTC, <b>Seconds</b>)</para>
    ///
    /// <para>When this file was deleted from the Storage Bucket</para>
    /// </summary>
    public long? DeletedAt { get; set; }
    
    /// <summary>
    /// <para><b>Stored as ulong</b></para>
    ///
    /// <inheritdoc cref="GetDeletedByUserId()"/>
    /// </summary>
    public string? DeletedByUserId { get; set; }

    /// <summary>
    /// <para>User Id for the User that deleted this file.</para>
    ///
    /// <para>Will only be null when this file hasn't been marked for deletion</para>
    /// </summary>
    public ulong? GetDeletedByUserId()
    {
        return DeletedByUserId == null ? null : ulong.Parse(DeletedByUserId!);
    }
    
    /// <summary>
    /// <para><b>UNIX Timestamp (UTC, <b>Seconds</b>)</para>
    ///
    /// <para>Timestamp when this record was last updated</para>
    /// </summary>
    public long Timestamp { get; set; }

    public EvidenceFileModel()
        : base()
    {
        Filename = "";
        Description = "";
        ContentType = "application/octet-stream";
        Size = "0";
        
        UploadedByUserId = "0";
        GuildId = "0";

        ObjectLocation = "";

        IsDeleted = false;
        IsMarkedForDeletion = false;
        MarkedForDeletedAt = null;
        DeletedAt = null;
        DeletedByUserId = null;
    }
}