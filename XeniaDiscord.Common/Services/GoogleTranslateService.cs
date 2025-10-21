using Google.Apis.Auth.OAuth2;
using Google.Cloud.Translation.V2;
using XeniaBot.Shared;
using XeniaBot.Shared.Config;
using XeniaDiscord.Common.Interfaces;

namespace XeniaDiscord.Common.Services;

public class GoogleTranslateService : IGoogleTranslateService, IXeniaService
{
    private TranslationClient? _client;
    public Task OnDiscordReady() => Task.CompletedTask;
    public async Task ActivateAsync()
    {
        var credentials = GoogleCredential.FromJson(XeniaConfig.Get().ApiKey.GoogleCloudTranslateAsBase64 ?? "{}");
        _client = await TranslationClient.CreateAsync(credentials);
    }

    public ICollection<Language> GetLanguages()
    {
        if (_client == null) ActivateAsync().Wait();
        return _client!.ListLanguages("en").ToList();
    }
    public async Task<TranslationResult> Translate(string content, string toLanguage = "en", string? fromLanguage = null)
    {
        if (_client == null) await ActivateAsync();
        return await _client!.TranslateTextAsync(content, toLanguage, fromLanguage);
    }
}
