using Discord;
using Discord.Interactions;
using Google.Cloud.Translation.V2;
using Microsoft.Extensions.DependencyInjection;
using XeniaDiscord.Common.Interfaces;
using XeniaDiscord.Shared.Interactions.Handlers;

namespace XeniaDiscord.Shared.Interactions.Modules;

public class TranslateModule : InteractionModuleBase
{
    private readonly IGoogleTranslateService _translate;
    public TranslateModule(IServiceProvider services)
    {
        _translate = services.GetRequiredService<IGoogleTranslateService>();
    }

    [SlashCommand("translate", "Translate anything to whatever language you want")]
    public async Task Translate(
        [Summary(description: "Target phrase to translate")]
        string phrase,
        [Summary(name: "language_output", description: "Language to translate to"), Autocomplete(typeof(GoogleTranslateAutocompleteHandler))] string targetLanguage="en",
        [Summary("language_input", description: "Language to translate from. Will detect when none provided."), Autocomplete(typeof(GoogleTranslateAutocompleteHandler))] string? sourceLanguage=null)
    {
        if (targetLanguage == "null" || targetLanguage.Length < 1)
            targetLanguage = "en";
        if (sourceLanguage == "null" || sourceLanguage?.Length < 1)
            sourceLanguage = null;
        TranslationResult? result = null;
        try
        {
            result = await _translate.Translate(phrase, targetLanguage, sourceLanguage);
        }
        catch (Exception ex)
        {
            var failEmbed = new EmbedBuilder()
            {
                Title = "Failed to translate!",
                Description = ex.Message,
                Color = new Color(255, 0 ,0)
            };
            await Context.Interaction.RespondAsync(embed: failEmbed.Build());
            // await DiscordHelper.ReportError(ex, Context);
            return;
        }
        if (result == null)
            return;

        var embed = new EmbedBuilder()
        {
            Description = $"Detected language as {result.DetectedSourceLanguage ?? result.SpecifiedSourceLanguage ?? "null"}",
        };
        embed.AddField($"From ({result.SpecifiedSourceLanguage ?? result.DetectedSourceLanguage})", result.OriginalText);
        embed.AddField($"To ({result.TargetLanguage})", result.TranslatedText);
        embed.WithFooter("Translated with Google Cloud Translate API");
        await Context.Interaction.RespondAsync(embed: embed.Build());
    }
}
