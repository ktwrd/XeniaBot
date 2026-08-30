using Discord;
using Discord.Interactions;
using Google.Cloud.Translation.V2;
using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Core.Helpers;
using XeniaBot.Core.Services.Wrappers;
using XeniaBot.Shared;
using XeniaBot.Shared.Helpers;

namespace XeniaBot.Core.Modules;

public class TranslateModule : InteractionModuleBase
{
    private readonly GoogleTranslateService _service;

    public TranslateModule(IServiceProvider services)
    {
        _service = services.GetRequiredService<GoogleTranslateService>();
    }
    
    [SlashCommand("translate", "Translate anything to whatever language you want")]
    [RegisterDBLCommand]
    [UsedImplicitly]
    public async Task Translate(
        [Summary(description: "Target phrase to translate")]
        string phrase,
        [Summary(name: "language_output", description: "Language to translate to"), Autocomplete(typeof(GoogleTranslateAutocompleteHandler))] string targetLanguage="en",
        [Summary("language_input", description: "Language to translate from. Will detect when none provided."), Autocomplete(typeof(GoogleTranslateAutocompleteHandler))] string? sourceLanguage=null)
    {
        if (targetLanguage == "null" || string.IsNullOrWhiteSpace(targetLanguage))
            targetLanguage = "en";
        if (sourceLanguage == "null" || string.IsNullOrWhiteSpace(sourceLanguage))
            sourceLanguage = null;
        TranslationResult? result = null;

        await DeferAsync();
        try
        {
            result = await _service.Translate(phrase, targetLanguage, sourceLanguage)
                ?? throw new Exception("No result!");
        }
        catch (Exception ex)
        {
            var failEmbed = new EmbedBuilder()
            {
                Title = "Failed to translate!",
                Description = ex.Message.Length > 4000 ? ex.Message[..4000] + "..." : ex.Message,
                Color = new Color(255, 0 ,0)
            };
            await ExceptionHelper.RetryOnTimedOut(async () => await FollowupAsync(embed: failEmbed.Build()));
            await DiscordHelper.ReportError(ex, Context);
            return;
        }

        var embed = new EmbedBuilder()
        {
            Description = $"Detected language as {result.DetectedSourceLanguage ?? result.SpecifiedSourceLanguage ?? "null"}",
        };
        embed.AddField($"From ({result.SpecifiedSourceLanguage ?? result.DetectedSourceLanguage})", result.OriginalText);
        embed.AddField($"To ({result.TargetLanguage})", result.TranslatedText);
        embed.WithFooter("Translated with Google Cloud Translate API");
        await ExceptionHelper.RetryOnTimedOut(async () => await FollowupAsync(embed: embed.Build()));
    }
}
