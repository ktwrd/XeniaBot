using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace XeniaDiscord.Shared.Interactions.Handlers;

public class WeatherApiAutocompleteHandler : AutocompleteHandler
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
        IInteractionContext context,
        IAutocompleteInteraction autocompleteInteraction,
        IParameterInfo parameter,
        IServiceProvider services)
    {
        var service = services.GetRequiredService<IWeatherApiService>();

        var results = new List<AutocompleteResult>();
        var currentSearch = autocompleteInteraction.Data.Current.Value.ToString();
        try
        {
            var response = await service.SearchAutocomplete(currentSearch);
            if (response.Error != null)
            {
                switch (response.Error.Code)
                {
                    case 1003:
                        return AutocompletionResult.FromError(InteractionCommandError.Unsuccessful, $"Not found ({response.Error.Code})");
                    default:
                        return AutocompletionResult.FromError(InteractionCommandError.Unsuccessful, $"{response.Error.Code}: {response.Error.Message}");
                }
            }
            if (response.Response != null)
            {
                foreach (var item in response.Response)
                {
                    results.Add(new AutocompleteResult($"{item.Name}, {item.Region}", item.Url));
                }
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to run autocomplete");
            return AutocompletionResult.FromError(InteractionCommandError.Unsuccessful, ex.Message);
        }

        return AutocompletionResult.FromSuccess(results.Take(25));
    }
}
