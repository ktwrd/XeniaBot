using System.Text;
using System.Text.Json;
using Discord;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using kate.shared.Helpers;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Evidence.Models;
using XeniaBot.Evidence.Repositories;
using XeniaBot.Shared;
using XeniaBot.Shared.Helpers;
using XeniaBot.Shared.Services;

namespace XeniaBot.Evidence.Services;

[XeniaController]
public class EvidenceFileService : BaseService
{
    private readonly EvidenceFileRepository _fileRepo;
    private readonly ConfigData _configData;
    private readonly ErrorReportService _errorReport;
    public EvidenceFileService(IServiceProvider services)
        : base(services)
    {
        _fileRepo = services.GetRequiredService<EvidenceFileRepository>();
        _configData = services.GetRequiredService<ConfigData>();
        _errorReport = services.GetRequiredService<ErrorReportService>();
    }

    #region Initialize
    public override async Task InitializeAsync()
    {
        var start_ts = GeneralHelper.GetMicroseconds();
        await CreateClient();
        Log.Debug($"Done! Took {GeneralHelper.GetMicroseconds() - start_ts}us");
    }
    
    /// <summary>
    /// Is set from <see cref="CreateClient"/>
    /// </summary>
    private StorageClient? _storageClient;
    private async Task CreateClient()
    {
        if (_configData.EvidenceService?.Enable == false)
        {
            Log.Warn("Evidence File Service is disabled. Ignoring");
            return;
        }
        var credentials = await LoadCredentials();
        var client = await StorageClient.CreateAsync(credentials);
        if (client == null)
        {
            Log.Error($"StorageClient is null? (Failed to create client)");
            CoreContext.Instance!.OnQuit(1);
            return;
        }

        _storageClient = client;
    }
    
    private async Task<GoogleCredential?> LoadCredentials()
    {
        var configGcs = _configData.EvidenceService?.GetGoogleCloudKey();
        
        bool denyAccess = configGcs == null || configGcs.ProjectId.Length < 1;
        if (denyAccess)
        {
            Log.Error("Config not setup (null or project_id not set)");
            CoreContext.Instance!.OnQuit(1);
        }

        GoogleCredential? cred = null;
        using (CancellationTokenSource source = new CancellationTokenSource())
        {
            var jsonText = JsonSerializer.Serialize(configGcs, XeniaHelper.SerializerOptions);
            if (jsonText == null)
            {
                Log.Error("Failed to serialize Google Cloud Config.");
                CoreContext.Instance!.OnQuit(1);
                return null;
            }
            using (var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(jsonText)))
            {
                try
                {
                    cred = await GoogleCredential.FromStreamAsync(memoryStream, source.Token);
                }
                catch (Exception ex)
                {
                    string errorMessage = "Failed to create GoogleCredential";
#if DEBUG
                    throw;
#endif
                    Log.Error(errorMessage);
                    Log.Error(ex);
                    CoreContext.Instance!.OnQuit(1);
                }
            }
        }
        if (cred == null)
        {
            string errorMessage = "Object \"cred\" is null. (Failed to create credentials)";
#if DEBUG
            throw new Exception(errorMessage);
#endif
            Log.Error(errorMessage);
            CoreContext.Instance!.OnQuit(1);
            return null;
        }
        return cred;
    }
    #endregion
    
    /// <summary>
    /// Result codes for when a file has uploaded.
    /// </summary>
    public enum UploadFileResultCode
    {
        /// <summary>
        /// A generic failure has occurred. The error was reported via <see cref="ErrorReportService"/>
        /// </summary>
        GenericFailure,
        /// <summary>
        /// File uploaded successfully!
        /// </summary>
        Ok,
        /// <summary>
        /// Evidence Service is disabled.
        /// </summary>
        Disabled,
        /// <summary>
        /// File is too large.
        /// </summary>
        TooLarge,
        /// <summary>
        /// Requesting Discord User doesn't have the correct permissions to upload evidence.
        /// </summary>
        MissingPermission
    }

    public bool WriteFileToStream(EvidenceFileModel model, Stream targetStream, out string? error)
    {
        error = null;
        if (_configData.EvidenceService?.Enable == false)
        {
            Log.Error("Evidence File Service is disabled.");
            error = "Evidence File Service is disabled.";
            return false;
        }
        
        if (_storageClient == null)
        {
            CreateClient().Wait();
        }

        var o = _storageClient?.GetObject(_configData.EvidenceService!.BucketName, model.ObjectLocation);
        if (o == null)
        {
            error = "Object does not exist in bucket";
            return false;
        }
        _storageClient!.DownloadObject(o, targetStream);
        return true;
    }
    
    #region Upload File
    /// <summary>
    /// Upload a file by specifying all the required parameters.
    /// </summary>
    /// <param name="stream">Stream where the content to upload lives.</param>
    /// <param name="filename">Name of the file</param>
    /// <param name="contentType">MIME Content type of the file</param>
    /// <param name="size">Size of the file.</param>
    /// <param name="author">User who wants to upload some evidence.</param>
    /// <param name="description">Description for the file</param>
    public async Task<(EvidenceFileModel?, UploadFileResultCode)> UploadFile(
        Stream stream,
        string filename,
        string contentType,
        ulong size,
        IGuildUser author,
        string description = "")
    {
        if (_configData.EvidenceService?.Enable == false)
        {
            Log.Error("Evidence File Service is disabled.");
            return (null, UploadFileResultCode.Disabled);
        }

        if (author.GuildPermissions.Has(GuildPermission.ManageGuild) == false)
        {
            return (null, UploadFileResultCode.MissingPermission);
        }
        
        if (size > _configData.EvidenceService!.MaximumUploadSize)
        {
            return (null, UploadFileResultCode.Disabled);
        }
        
        if (_storageClient == null)
        {
            await CreateClient();
        }
        
        var bucketName = _configData.EvidenceService!.BucketName;

        var fileModel = new EvidenceFileModel()
        {
            Filename = filename,
            Description = description,
            ContentType = contentType,
            Size = size.ToString(),
            UploadedByUserId = author.Id.ToString(),
            GuildId = author.Guild.Id.ToString(),
            UploadedAtTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        fileModel.ObjectLocation = $"{fileModel.Id}/{filename}";
        
        var bucketObject = await _storageClient!.UploadObjectAsync(
            bucketName,
            fileModel.ObjectLocation,
            contentType,
            stream);
        if (bucketObject?.Size != null)
        {
            fileModel.Size = bucketObject!.Size!.ToString() ?? fileModel.Size;
            if (fileModel.GetSize() > _configData.EvidenceService!.MaximumUploadSize)
            {
                try
                {
                    await _storageClient!.DeleteObjectAsync(
                        bucketName,
                        fileModel.ObjectLocation);
                }
                catch (Exception ex)
                {
                    var errorMessage = $"Failed to delete object when it was detected as too large!\n" +
                                       $"Id: {fileModel.Id}\n" +
                                       $"ObjectLocation: {fileModel.ObjectLocation}";
                    Log.Error(errorMessage);
                    Log.Error(ex.ToString());
                    try
                    {
                        await _errorReport.ReportException(ex, errorMessage);
                    }
                    catch (Exception iex)
                    {
                        Log.Error("Failed to report error!");
                        Log.Error(iex.ToString());
                    }
                    return (null, UploadFileResultCode.GenericFailure);
                }
                return (null, UploadFileResultCode.TooLarge);
            }
        }

        fileModel = await _fileRepo.InsertOrUpdate(fileModel);
        return (fileModel, UploadFileResultCode.Ok);
    }
    
    /// <summary>
    /// Upload a file from a Discord Attachment.
    /// </summary>
    /// <param name="attachment">Discord Attachment</param>
    /// <param name="author">User who wants to upload some evidence.</param>
    /// <param name="description">Description for the file.</param>
    public async Task<(EvidenceFileModel?, UploadFileResultCode)> UploadFile(IAttachment attachment, IGuildUser author, string description = "")
    {
        if (_configData.EvidenceService?.Enable == false)
        {
            Log.Error("Evidence File Service is disabled.");
            return (null, UploadFileResultCode.Disabled);
        }

        if (author.GuildPermissions.Has(GuildPermission.ManageGuild) == false)
        {
            return (null, UploadFileResultCode.MissingPermission);
        }
        
        var attachmentSize = ulong.Parse(Math.Max(0, attachment.Size).ToString());

        if (attachmentSize > _configData.EvidenceService!.MaximumUploadSize)
        {
            Log.Error($"{attachment.Id} too large ({attachment.Size})");
            return (null, UploadFileResultCode.Disabled);
        }

        if (_storageClient == null)
        {
            await CreateClient();
        }
        
        // fetch content from url and get the reported length
        // of the content
        var client = new HttpClient();
        var httpResponse = await client.GetAsync(attachment.Url);
        ulong? contentLength = httpResponse.Content.Headers.ContentLength == null
            ? null
            : ulong.Parse(Math.Max(0, httpResponse.Content.Headers.ContentLength ?? 0).ToString());
        if ((contentLength ?? attachmentSize) > _configData.EvidenceService.MaximumUploadSize)
        {
            httpResponse.Dispose();
            client.Dispose();
            return (null, UploadFileResultCode.TooLarge);
        }
        
        // async overload not used because it *can* cause issues.
        var responseBody = httpResponse.Content.ReadAsStream();
        // copy to MemoryStream so we don't accidentally double-read
        // the stream in the future. also so we can calculate the
        // real length of the content the user wants to upload.
        var ms = new MemoryStream();
        responseBody.CopyTo(ms);
        var realLength = ulong.Parse(Math.Max(0, ms.Length).ToString());

        void LocalDispose()
        {
            try
            {
                ms.Dispose();
                responseBody.Dispose();
                httpResponse.Dispose();
                client.Dispose();
            }
            catch (Exception ex)
            {
                Log.Warn($"Failed to dispose");
                Log.Warn(ex);
            }
        }

        // double check that the size of the data we are uploading
        // *is* within the allowed limits.
        if (realLength > _configData.EvidenceService.MaximumUploadSize)
        {
            LocalDispose();
            return (null, UploadFileResultCode.TooLarge);
        }

        try
        {
            var res = await UploadFile(
                ms,
                attachment.Filename,
                attachment.ContentType,
                realLength,
                author,
                description);
            LocalDispose();
            return res;
        }
        catch (Exception ex)
        {
            var errorMessage =
                $"Failed to upload attachment {attachment.Id} from user {author} ({author.Id}) in guild {author.Guild.Name} ({author.Guild.Id})";
            Log.Error(errorMessage);
            Log.Error(ex);
            try
            {
                await _errorReport.ReportException(ex, errorMessage);
            }
            catch (Exception iex)
            {
                Log.Error($"Failed to report exception!!!!!");
                Log.Error(iex.ToString());
            }
            LocalDispose();
            return (null, UploadFileResultCode.GenericFailure);
        }
    }
    #endregion
}