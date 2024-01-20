using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Threading;
using Google.Cloud.Storage.V1;
using kate.shared.Helpers;
using XeniaBot.Shared;
using XeniaBot.Shared.Controllers;

namespace XeniaBot.Core.Controllers.Wrappers;

[BotController]
public class GoogleCloudStorageController : BaseController
{
    private readonly ConfigData _configData;
    private GoogleCredential _gcsCred;
    private StorageClient _storageClient;
    private ErrorReportController _err;
    public GoogleCloudStorageController(IServiceProvider services)
        : base(services)
    {
        _configData = services.GetRequiredService<ConfigData>();
        _err = services.GetRequiredService<ErrorReportController>();
    }

        private async Task<GoogleCredential?> LoadCredentials()
        {
            bool denyAccess = _configData.GoogleCloud == null || _configData.GoogleCloud.ProjectId.Length < 1;
            if (denyAccess)
            {
                Log.Error("Config not setup (null or project_id not set)");
                Program.Quit(1);
            }

            GoogleCredential? cred = null;
            using (CancellationTokenSource source = new CancellationTokenSource())
            {
                var jsonText = JsonSerializer.Serialize(_configData.GoogleCloud, Program.SerializerOptions);
                if (jsonText == null)
                {
                    Log.Error("Failed to serialize Google Cloud Config.");
                    Program.Quit(1);
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
                        Program.Quit(1);
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
                Program.Quit(1);
                return null;
            }
            return cred;
        }
    private void ValidateConfig()
    {
        
        bool configInvalid =
            _configData.GoogleCloud == null;
        var configDictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(
            JsonSerializer.Serialize(
                _configData.GoogleCloud ?? new GoogleCloudKey(),
                Program.SerializerOptions) ?? "{}",
            Program.SerializerOptions) ?? new Dictionary<string, string>();
        int dictRequired = configDictionary.Count;
        int dictFound = 0;

        if (dictRequired < 1)
        {
            Log.Error("Config.GoogleCloud is empty");
            Program.Quit(1);
        }
        foreach (var pair in configDictionary)
        {
            if (pair.Value.Length > 0)
                dictFound++;
            else
                Log.Warn($"Empty value \"Config.GoogleCloud.{pair.Key}\"");
        }
        if (dictFound < dictRequired)
        {
            Log.Error("Not all fields have a value in \"Config.GoogleCloud\"");
            configInvalid = false;
        }

        if (configInvalid)
        {
            Log.Error("Config Invalid");
            Program.Quit(1);
            return;
        }
    }

    public override async Task InitializeAsync()
    {
        if (_configData.AttachmentArchive.Enable && _configData.AttachmentArchive.BucketName == null)
        {
            Log.Error($"ConfigData.AttachmentArchive.BucketName must be set when this feature is enabled");
            Environment.Exit(1);
        }
        
        var start_ts = GeneralHelper.GetMicroseconds();
        Log.Debug("Initializing GoogleTranslate");
        ValidateConfig();

        // Fetch credentials, if failed then throw exception
        GoogleCredential? googleCredential = await LoadCredentials();
        if (googleCredential == null)
        {
            Log.Error("Failed to load Google Cloud Translate Credentials. Result was null");
            Program.Quit(1);
        }
        _gcsCred = googleCredential;

        await CreateClient();
        Log.Debug($"Done! Took {GeneralHelper.GetMicroseconds() - start_ts}us");
    }

    private async Task CreateClient()
    {
        var client = await StorageClient.CreateAsync(_gcsCred);
        if (client == null)
        {
            Log.Error("TranslationClient is null? (Failed to create client)");
            Program.Quit(1);
            return;
        }
        _storageClient = client;
    }

    public async Task<Object> UploadFile(string path, byte[] data, string contentType)
    {
        var bucket = await _storageClient.GetBucketAsync(_configData.AttachmentArchive.BucketName);
        try
        {
            var obj = await _storageClient.UploadObjectAsync(
                _configData.AttachmentArchive.BucketName,
                path,
                contentType,
                new MemoryStream(data));
            return obj;
        }
        catch (Exception ex)
        {
            await _err.ReportException(ex, $"Failed to upload file to {path}");
            throw;
        }
    }
}