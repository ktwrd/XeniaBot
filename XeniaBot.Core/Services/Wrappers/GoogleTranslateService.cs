using Google.Apis.Auth.OAuth2;
using Google.Cloud.Translation.V2;
using kate.shared.Helpers;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using XeniaBot.Shared.Helpers;

namespace XeniaBot.Core.Services.Wrappers;

[XeniaController]
public class GoogleTranslateService : BaseService
{
    private readonly Logger _log = LogManager.GetLogger("Xenia." + nameof(GoogleTranslateService));
    private readonly ConfigData _configData;
    private GoogleCredential? _googleCredential = null;
    private TranslationClient? _translateClient = null;
    public GoogleTranslateService(IServiceProvider services)
        : base(services)
    {
        _configData = services.GetRequiredService<ConfigData>();
    }

    private void Validate()
    {
        var configInvalid = _configData.GoogleCloud == null;
        var configDictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(
            JsonSerializer.Serialize(
                _configData.GoogleCloud ?? new GoogleCloudKey(),
                Program.SerializerOptions) ?? "{}",
            Program.SerializerOptions) ?? new Dictionary<string, string>();
        var dictRequired = configDictionary.Count;
        var dictFound = 0;

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
                _log.Warn($"Empty value in config: \"GCSKey_Translate.{pair.Key}\"");
        }
        if (dictFound < dictRequired)
        {
            _log.Error("Not all fields have a value in config: \"GCSKey_Translate\"");
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

        return await _translateClient.TranslateTextAsync(message, to, from);
    }

    private async Task<GoogleCredential?> LoadCredentials()
    {
        if (_configData.GoogleCloud == null)
        {
            throw new InvalidOperationException("GoogleCloud not configured in config");
        }
        if (string.IsNullOrEmpty(_configData.GoogleCloud.ProjectId?.Trim()))
        {
            throw new InvalidOperationException("ProjectId not configured in GoogleCloud (from config)");
        }

        GoogleCredential? credential;
        using (var source = new CancellationTokenSource())
        {
            var jsonText = JsonSerializer.Serialize(_configData.GoogleCloud, SerializerOptions)
                ?? throw new InvalidOperationException("Failed to serialize GoogleCloud credentials in config file (result was null)");
            using var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(jsonText));
            try
            {
                credential = (await CredentialFactory.FromStreamAsync<ServiceAccountCredential>(memoryStream, source.Token)).ToGoogleCredential();
            }
            catch (Exception ex)
            {
                const string errorMessage = "Failed to create GoogleCredential";
                _log.Fatal(ex, errorMessage);
                Debugger.BreakForUserUnhandledException(ex);
                throw new InvalidOperationException(errorMessage, ex);
            }
        }
        if (credential == null)
        {
            var errorMessage = $"Failed to create credentials: {typeof(CredentialFactory).Namespace}.{nameof(CredentialFactory)}.{nameof(CredentialFactory.FromStreamAsync)} returned null";
            _log.Error(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }
        return credential;
    }
    private static readonly JsonSerializerOptions SerializerOptions
        = new()
        {
            IgnoreReadOnlyFields = false,
            IgnoreReadOnlyProperties = false,
            IncludeFields = true,
            WriteIndented = true
        };

    private async Task CreateClient()
    {
        var client = await TranslationClient.CreateAsync(_googleCredential);
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
        var trans = SentryHelper.CreateTransaction();
        try
        {
            var startedAt = GeneralHelper.GetMicroseconds();
            _log.Debug("Initializing GoogleTranslateService");
            Validate();

            // Fetch credentials, if failed then throw exception
            var googleCredential = await LoadCredentials();
            if (googleCredential == null)
            {
                _log.Error("Failed to load Google Cloud Translate Credentials. Result was null");
                trans.Finish();
                Program.Quit(1);
                return;
            }
            _googleCredential = googleCredential;

            await CreateClient();
            _log.Debug($"Done! Took {GeneralHelper.GetMicroseconds() - startedAt}us");
        }
        finally
        {
            trans.Finish();
        }
    }
}
