using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using SkidBot.Core.Controllers.Wrappers;
using SkidBot.Core.Helpers;
using SkidBot.Shared.Helpers;
using SkidBot.Shared.Schema.WeatherAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkidBot.Core.Modules
{
    [Group("weather", "Get info about the weather")]
    public class WeatherModule : InteractionModuleBase
    {
        [SlashCommand("get", "Fetch weather")]
        public async Task Fetch([Summary("weather_location"), Autocomplete(typeof(WeatherAPIAutocompleteHandler))] string location, MeasurementSystem syst)
        {
            var resultEmbed = await WeatherEmbedHelper.CurrentForecast(location, syst);
            await Context.Interaction.RespondAsync(embed: resultEmbed.Build());
        }

        [SlashCommand("forecast", "Fetch 3 day weather forecast")]
        public async Task Forecast([Summary("weather_location"), Autocomplete(typeof(WeatherAPIAutocompleteHandler))] string location, MeasurementSystem syst)
        {
            var resultEmbed = await WeatherEmbedHelper.Forecast(location, syst);
            await Context.Interaction.RespondAsync(embed: resultEmbed.Build());
        }
    }
}
