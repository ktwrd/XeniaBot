using System;
using MongoDB.Bson;
using XeniaBot.Shared.Models;

namespace XeniaBot.Data.Models;

public class AttachmentModel : BaseModelGuid
{
    public const string CollectionName = "attachment";
    public AttachmentModel()
        : base()
    {
        CreatedAt = new BsonTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        Filename = "attachment";
        CreatedByUserId = "0";
        SetFileSize(0);
    }

    /// <summary>
    /// Relative location in the S3 Bucket for this file.
    /// </summary>
    public string RelativePath { get; set; }
    /// <summary>
    /// Name of the file
    /// </summary>
    public string Filename { get; set; }
    
    /// <summary>
    /// Mime Type of the file.
    /// </summary>
    public string? MimeType { get; set; }

    /// <summary>
    /// Discord User Id that uploaded this file.
    /// </summary>
    public string CreatedByUserId { get; set; }
    
    
    /// <summary>
    /// When the file was created or uploaded.
    /// </summary>
    public BsonTimestamp CreatedAt { get; set; }

    /// <summary>
    /// Stored as <see cref="long"/>. Use <see cref="GetFileSize"/> to actually get the content.
    /// </summary>
    public string FileSize { get; set; }

    public long GetFileSize()
    {
        if (long.TryParse(FileSize, out long result))
            return result;

        return 0;
    }

    public void SetFileSize(long value)
    {
        FileSize = value.ToString();
    }
}