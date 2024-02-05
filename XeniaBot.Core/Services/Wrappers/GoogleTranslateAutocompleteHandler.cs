using Discord;
using Discord.Interactions;
using Google.Cloud.Translation.V2;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XeniaBot.Core.Services.Wrappers
{
    public class GoogleTranslateAutocompleteHandler : AutocompleteHandler
    {
        private List<Language>? languageList = null;
        public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
        {
            var service = Program.Core.GetRequiredService<GoogleTranslateService>();

            var results = new List<AutocompleteResult>();
            var currentSearch = autocompleteInteraction.Data.Current.Value?.ToString()?.Trim().ToLower() ?? "";
            results.Add(new AutocompleteResult("Auto", ""));
            try
            {
                List<Language> response = languageList ?? service.GetLanguages();
                foreach (var item in response)
                {
                    if (item.Code == null)
                        continue;
                    string codeTrim = item.Code.Trim().ToLower();
                    string nameTrim = item.Name?.Trim()?.ToLower() ?? codeTrim;
                    if (codeTrim == null || nameTrim == null)
                        continue;
                    bool codeContains = codeTrim.Contains(currentSearch);
                    bool nameContains = nameTrim.Contains(currentSearch);
                    if (codeContains || nameContains)
                    {
                        results.Add(new AutocompleteResult(item.Name, item.Code));
                    }
                }
            }
            catch (Exception ex)
            {
                await DiscordHelper.ReportError(ex);
                return AutocompletionResult.FromError(InteractionCommandError.Unsuccessful, ex.Message);
            }

            return AutocompletionResult.FromSuccess(results.Take(25));
        }
    }
}
