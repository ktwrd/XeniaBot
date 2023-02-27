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
            var controller = Program.Services.GetRequiredService<WeatherAPIController>();
            var embed = new EmbedBuilder()
            {
                Color = Color.Red
            }.WithCurrentTimestamp();
            
            if (controller == null)
            {
                Log.Error($"WeatherAPIController is null!");
                embed.Description = "WeatherAPIController is null!";
                await Context.Interaction.RespondAsync(embed: embed.Build());
                return;
            }

            WeatherResponse? result = null;
            try
            {
                result = await controller.FetchCurrent(location);
            }
            catch (Exception ex)
            {
                embed.Description = $"Exception occurred; `{ex.Message}`";
                await Context.Interaction.RespondAsync(embed: embed.Build());
                await DiscordHelper.ReportError(ex, Context);
                return;
            }
            if (result == null)
            {
                embed.Description = $"Invalid response from server (null)";
                await Context.Interaction.RespondAsync(embed: embed.Build());
                return;
            }

            if (result.Error != null)
            {
                switch (result.Error.Code)
                {
                    case 1003:
                        embed.Description = $"Location not found ({result.Error.Code})";
                        break;
                    default:
                        embed.Description = $"Failed to fetch weather\n```\n{result.Error.Code}: {result.Error.Message}\n```";
                        break;
                }
                await Context.Interaction.RespondAsync(embed: embed.Build());
                return;
            }
            if (result.Current == null)
            {
                embed.Description = $"Got null result from controller";
                await Context.Interaction.RespondAsync(embed: embed.Build());
                return;
            }
            if (result.Location == null)
            {
                embed.Description = $"Location data is missing";
                await Context.Interaction.RespondAsync(embed: embed.Build());
                return;
            }

            embed = new EmbedBuilder()
            {
                Title = $"Weather in {result.Location.Name}, {result.Location.Region}",
                Url = $"https://google.com/maps/@{result.Location.Longitude},{result.Location.Latitude}",
                ImageUrl = "https://" + result.Current.Condition?.IconUrl.Split("//")[1],
                Color = Color.Blue
            }.WithCurrentTimestamp();
            var fields = new List<(string, string)>()
            {
                ("Temperature", syst == MeasurementSystem.Metric ? $"{result.Current.TemperatureCelcius}°C" : $"{result.Current.TemperatureFahrenheit}°F"),
                ("Feels like", syst == MeasurementSystem.Metric ? $"{result.Current.TemperatureFeelsLikeCelcius}°C" : $"{result.Current.TemperatureFeelsLikeFahrenheit}°F"),
                ("Wind Speed", syst == MeasurementSystem.Metric ? $"{result.Current.WindSpeedKph}KPH" : $"{result.Current.WindSpeedMph}MPH"),
                ("Wind Direction", result.Current.WindDirection),
                ("Humidity", $"{result.Current.Humidity}%"),
                ("Cloud Coverage", $"{result.Current.CloudCoverage}"),
                ("Visibility", syst == MeasurementSystem.Metric ? $"{result.Current.VisibilityKm}KM" : $"{result.Current.VisiblityMiles}Mi"),
                ("UV Index", result.Current.UV.ToString()),
                ("Percipitation", syst == MeasurementSystem.Metric ? $"{result.Current.PrecipitationMm}mm" : $"{result.Current.PrecipitationIn}in")
            };
            foreach (var (name, value) in fields)
            {
                embed.AddField(name, value, true);
            }

            await Context.Interaction.RespondAsync(embed: embed.Build());
        }
    }
}
