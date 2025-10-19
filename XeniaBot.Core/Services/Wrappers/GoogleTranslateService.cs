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
using XeniaBot.Shared.Config;
using NLog;

namespace XeniaBot.Core.Services.Wrappers
{
    public class GoogleTranslateService
    {
        private XeniaConfig _config;
        private GoogleCredential _gcsCred;
        private TranslationClient _translateClient;
        private readonly Logger _log = LogManager.GetCurrentClassLogger();
        public GoogleTranslateService(IServiceProvider services)
        {
            _config = services.GetRequiredService<XeniaConfig>();
        }

        private void Validate()
        {
            // TODO re-implement config loading for XML
            bool configInvalid = _config.GoogleCloud == null;
            var configDictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(
                JsonSerializer.Serialize(
                    _config.GoogleCloud ?? new GoogleCloudKey(),
                    Program.SerializerOptions) ?? "{}",
                Program.SerializerOptions) ?? new Dictionary<string, string>();
            int dictRequired = configDictionary.Count;
            int dictFound = 0;

            if (dictRequired < 1)
            {
                _log.Error("Config.GCSKey_Translate is empty");
                Environment.Exit(1);
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
                Environment.Exit(1);
                return;
            }
        }
        public List<Language> GetLanguages()
        {
            if (_translateClient == null)
            {
                throw new Exception("Translation Client is null");
            }
            var result = _translateClient.ListLanguages("en").ToList();
            return result;
        }

        public async Task<TranslationResult> Translate(string message, string to="en", string? from=null)
        {
            if (_translateClient == null)
            {
                throw new Exception("Translation Client is null");
            }

            TranslationResult result = await _translateClient.TranslateTextAsync(message, to, from); ;
            return result;
        }

        private async Task<GoogleCredential?> LoadCredentials()
        {
            bool denyAccess = _config.GoogleCloud == null || _config.GoogleCloud.ProjectId.Length < 1;
            if (denyAccess)
            {
                Log.Error("Config not setup (null or project_id not set)");
                Program.Quit(1);
            }

            GoogleCredential? cred = null;
            using (CancellationTokenSource source = new CancellationTokenSource())
            {
                var jsonText = JsonSerializer.Serialize(_config.GoogleCloud, Program.SerializerOptions);
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

        private async Task CreateClient()
        {
            TranslationClient client = await TranslationClient.CreateAsync(_gcsCred);
            if (client == null)
            {
                _log.Error("TranslationClient is null? (Failed to create client)");
                Environment.Exit(1);
                return;
            }
            _translateClient = client;
        }

        public async Task InitializeAsync()
        {
            var start_ts = GeneralHelper.GetMicroseconds();
            _log.Debug("Initializing GoogleTranslateService");
            Validate();

            // Fetch credentials, if failed then throw exception
            var googleCredential = await LoadCredentials();
            if (googleCredential == null)
            {
                _log.Error("Failed to load Google Cloud Translate Credentials. Result was null");
                Environment.Exit(1);
                return;
            }
            _gcsCred = googleCredential;

            await CreateClient();
            _log.Debug($"Done! Took {GeneralHelper.GetMicroseconds() - start_ts}us");
        }
    }
}
