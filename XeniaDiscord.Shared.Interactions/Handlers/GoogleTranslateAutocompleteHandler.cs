using Discord;
using Discord.Interactions;
using Google.Cloud.Translation.V2;
using Microsoft.Extensions.DependencyInjection;
using XeniaDiscord.Common.Interfaces;

namespace XeniaDiscord.Shared.Interactions.Handlers;

public class GoogleTranslateAutocompleteHandler : AutocompleteHandler
{
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
        IInteractionContext context,
        IAutocompleteInteraction autocompleteInteraction,
        IParameterInfo parameter,
        IServiceProvider services)
    {
        var service = services.GetRequiredService<IGoogleTranslateService>();
        var results = new List<AutocompleteResult>();
        var currentSearch = autocompleteInteraction.Data.Current.Value?.ToString()?.Trim().ToLower() ?? "";
        results.Add(new AutocompleteResult("Auto", ""));
        try
        {
            foreach (var item in service.GetLanguages())
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
            return AutocompletionResult.FromError(InteractionCommandError.Unsuccessful, ex.Message);
        }

        return AutocompletionResult.FromSuccess(results.Take(25));
    }
}
