using Discord;
using Discord.Interactions;
using Google.Cloud.Translation.V2;
using Microsoft.Extensions.DependencyInjection;
using SkidBot.Core.Controllers.Wrappers;
using SkidBot.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkidBot.Core.Modules
{
    public class TranslateModule : InteractionModuleBase
    {
        [SlashCommand("translate", "Translate anything to whatever language you want")]
        public async Task Translate(
            string phrase,
            [Summary("language_output"), Autocomplete(typeof(GoogleTranslateAutocompleteHandler))] string targetLanguage="en",
            [Summary("language_input"), Autocomplete(typeof(GoogleTranslateAutocompleteHandler))] string? sourceLanguage=null)
        {
            if (targetLanguage == "null" || targetLanguage.Length < 1)
                targetLanguage = "en";
            if (sourceLanguage == "null" || sourceLanguage?.Length < 1)
                sourceLanguage = null;
            var controller = Program.Services.GetRequiredService<GoogleTranslate>();
            TranslationResult result = null;
            try
            {
                result = await controller.Translate(phrase, targetLanguage, sourceLanguage);
            }
            catch (Exception ex)
            {
                var failEmbed = new EmbedBuilder()
                {
                    Title = "Failed to translate!",
                    Description = ex.Message,
                    Color = new Color(255, 0 ,0)
                };
                await Context.Interaction.RespondAsync(embed: failEmbed.Build(), ephemeral: true);
                DiscordHelper.ReportError(ex, Context);
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
            await Context.Interaction.RespondAsync(embed: embed.Build(), ephemeral: true);
        }
    }
}
