using Google.Apis.Auth.OAuth2;
using Google.Cloud.Translation.V2;
using kate.shared.Helpers;
using Microsoft.Extensions.DependencyInjection;
using SkidBot.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SkidBot.Core.Controllers.Wrappers
{
    [SkidController]
    public class GoogleTranslate : BaseController
    {
        private ConfigManager.Config _config;
        private GoogleCredential _gcsCred;
        private TranslationClient _translateClient;
        public GoogleTranslate(IServiceProvider services)
            : base(services)
        {
            _config = services.GetRequiredService<ConfigManager.Config>();
        }

        private void Validate()
        {
            bool configInvalid =
                _config.GCSKey_Translate == null;
            var configDictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(
                JsonSerializer.Serialize(
                    _config.GCSKey_Translate ?? new ConfigManager.GoogleCloudKey(),
                    Program.SerializerOptions) ?? "{}",
                Program.SerializerOptions) ?? new Dictionary<string, string>();
            int dictRequired = configDictionary.Count;
            int dictFound = 0;

            if (dictRequired < 1)
            {
                Log.Error("Config.GCSKey_Translate is empty");
                Program.Quit(1);
            }
            foreach (var pair in configDictionary)
            {
                if (pair.Value.Length > 0)
                    dictFound++;
                else
                    Log.Warn($"Empty value \"Config.GCSKey_Translate.{pair.Key}\"");
            }
            if (dictFound < dictRequired)
            {
                Log.Error("Not all fields have a value in \"Config.GCSKey_Translate\"");
                configInvalid = false;
            }

            if (configInvalid)
            {
                Log.Error("Config Invalid");
                Program.Quit(1);
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
            bool denyAccess = _config.GCSKey_Translate == null || _config.GCSKey_Translate.ProjectId.Length < 1;
            if (denyAccess)
            {
                Log.Error("Config not setup (null or project_id not set)");
                Program.Quit(1);
            }

            GoogleCredential? cred = null;
            using (CancellationTokenSource source = new CancellationTokenSource())
            {
                var jsonText = JsonSerializer.Serialize(_config.GCSKey_Translate, Program.SerializerOptions);
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
                Log.Error("TranslationClient is null? (Failed to create client)");
                Program.Quit(1);
                return;
            }
            _translateClient = client;
        }

        public override async Task InitializeAsync()
        {
            var start_ts = GeneralHelper.GetMicroseconds();
            Log.Debug("Initializing GoogleTranslate");
            Validate();

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
    }
}
