using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using SkidBot.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkidBot.Core.Controllers.Wrappers
{
    public class WeatherAPIAutocompleteHandler : AutocompleteHandler
    {
        public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
        {
            var service = Program.Services.GetRequiredService<WeatherAPIController>();

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
                            break;
                        default:
                            return AutocompletionResult.FromError(InteractionCommandError.Unsuccessful, $"{response.Error.Code}: {response.Error.Message}");
                            break;
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
            catch(Exception ex)
            {
                await DiscordHelper.ReportError(ex);
                return AutocompletionResult.FromError(InteractionCommandError.Unsuccessful, ex.Message);
            }

            return AutocompletionResult.FromSuccess(results.Take(25));
        }
    }
}
