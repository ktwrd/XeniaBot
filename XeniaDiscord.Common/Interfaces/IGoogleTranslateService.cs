using Google.Cloud.Translation.V2;

namespace XeniaDiscord.Common.Interfaces;

public interface IGoogleTranslateService
{
    public ICollection<Language> GetLanguages();
    public Task<TranslationResult> Translate(string content, string toLanguage = "en", string? fromLanguage = null);
}
