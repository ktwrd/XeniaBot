using Google.Apis.Auth.OAuth2;
using Google.Cloud.Translation.V2;
using kate.shared.Helpers;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace XeniaBot.Core.Services.Wrappers;

[XeniaController]
public class GoogleTranslateService : BaseService
{
    private readonly Logger _log = LogManager.GetLogger("Xenia." + nameof(GoogleTranslateService));
    private ConfigData _configData;
    private GoogleCredential _gcsCred;
    private TranslationClient _translateClient;
    public GoogleTranslateService(IServiceProvider services)
        : base(services)
    {
        _configData = services.GetRequiredService<ConfigData>();
    }

    private void Validate()
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
            _log.Error("Config.GCSKey_Translate is empty");
            Program.Quit(1);
        }
        foreach (var pair in configDictionary)
        {
            if (pair.Value.Length > 0)
                dictFound++;
            else
                _log.Warn($"Empty value \"Config.GCSKey_Translate.{pair.Key}\"");
        }
        if (dictFound < dictRequired)
        {
            _log.Error("Not all fields have a value in \"Config.GCSKey_Translate\"");
            configInvalid = false;
        }

        if (configInvalid)
        {
            _log.Error("Config Invalid");
            Program.Quit(1);
        }
    }
    public List<Language> GetLanguages()
    {
        if (_translateClient == null)
        {
            throw new InvalidOperationException($"Translation Client ({nameof(_translateClient)} is null");
        }
        var result = _translateClient.ListLanguages("en").ToList();
        return result;
    }

    public async Task<TranslationResult> Translate(string message, string to="en", string? from=null)
    {
        if (_translateClient == null)
        {
            throw new InvalidOperationException($"Translation Client ({nameof(_translateClient)} is null");
        }

        TranslationResult result = await _translateClient.TranslateTextAsync(message, to, from); ;
        return result;
    }

    private async Task<GoogleCredential?> LoadCredentials()
    {
        bool denyAccess = _configData.GoogleCloud == null || _configData.GoogleCloud.ProjectId.Length < 1;
        if (denyAccess)
        {
            _log.Error("Config not setup (null or project_id not set)");
            Program.Quit(1);
        }

        GoogleCredential? cred = null;
        using (CancellationTokenSource source = new CancellationTokenSource())
        {
            var jsonText = JsonSerializer.Serialize(_configData.GoogleCloud, Program.SerializerOptions);
            if (jsonText == null)
            {
                _log.Error("Failed to serialize Google Cloud Config.");
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
                    var errorMessage = "Failed to create GoogleCredential";
                    _log.Error(ex, errorMessage);
#if DEBUG
                    throw;
#endif
                    Program.Quit(1);
                }
            }
        }
        if (cred == null)
        {
            string errorMessage = "Object \"cred\" is null. (Failed to create credentials)";
            _log.Error(errorMessage);
#if DEBUG
            throw new Exception(errorMessage);
#endif
            Program.Quit(1);
            return null;
        }
        return cred;
    }

    private async Task CreateClient()
    {
        TranslationClient client = await TranslationClient.CreateAsync(_gcsCred);
        if (client == null)
        {
            _log.Error("TranslationClient is null? (Failed to create client)");
            Program.Quit(1);
            return;
        }
        _translateClient = client;
    }

    public override async Task InitializeAsync()
    {
        var start_ts = GeneralHelper.GetMicroseconds();
        _log.Debug("Initializing GoogleTranslateService");
        Validate();

        // Fetch credentials, if failed then throw exception
        var googleCredential = await LoadCredentials();
        if (googleCredential == null)
        {
            _log.Error("Failed to load Google Cloud Translate Credentials. Result was null");
            Program.Quit(1);
            return;
        }
        _gcsCred = googleCredential;

        await CreateClient();
        _log.Debug($"Done! Took {GeneralHelper.GetMicroseconds() - start_ts}us");
    }
}
