using System;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Net;
using System.Text.Json;
using XeniaBot.Shared.Helpers;

namespace XeniaBot.Shared;

public class EvidenceServiceConfig
{
    public bool Enable { get; set; }
    /// <summary>
    /// Credentials
    /// </summary>
    public GoogleCloudKey? GoogleCloudKey { get; set; }
    /// <summary>
    /// When true, the content at <see cref="GoogleCloudKeyLocation"/> will be used. When false, <see cref="GoogleCloudKey"/> will be used.
    /// </summary>
    [DefaultValue(false)]
    public bool GetCloudKeyFromLocation { get; set; }
    /// <summary>
    /// <para>File location to where the Google Cloud Key is.</para>
    ///
    /// <para>Must be a JSON file and must be set when <see cref="GetCloudKeyFromLocation"/> is `true`.</para>
    /// </summary>
    public string? GoogleCloudKeyLocation { get; set; }

    /// <summary>
    /// Get the Google Cloud Key from either the Location specified or from <see cref="GoogleCloudKey"/>
    /// </summary>
    public GoogleCloudKey? GetGoogleCloudKey()
    {
        if (Enable == false)
            return null;
        if (GetCloudKeyFromLocation)
        {
            try
            {
                if (GoogleCloudKeyLocation == null)
                    throw new NoNullAllowedException($"{nameof(GoogleCloudKeyLocation)} is null");
                var content = File.ReadAllText(GoogleCloudKeyLocation);
                var data = JsonSerializer.Deserialize<GoogleCloudKey>(content, XeniaHelper.SerializerOptions);
                return data;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to read Cloud Key from file {GoogleCloudKeyLocation}", ex);
            }
        }
        else
        {
            return GoogleCloudKey;
        }
    }
    /// <summary>
    /// Maximum upload size for attachments (bytes)
    /// </summary>
    public ulong MaximumUploadSize { get; set; }
    /// <summary>
    /// Name of the bucket
    /// </summary>
    public string BucketName { get; set; }
    /// <summary>
    /// Base URL for where the bucket contents can be accessed.
    /// </summary>
    public string PublicEndpointUrl { get; set; }
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