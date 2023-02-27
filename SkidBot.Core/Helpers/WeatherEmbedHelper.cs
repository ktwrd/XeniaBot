using Discord;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using SkidBot.Core.Controllers.Wrappers;
using SkidBot.Shared.Helpers;
using SkidBot.Shared.Schema.WeatherAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MongoDB.Bson.Serialization.Serializers.SerializerHelper;
using static SkidBot.Core.Modules.WeatherModule;
using WHelper = SkidBot.Shared.Helpers.WeatherHelper;

namespace SkidBot.Core.Helpers
{
    public static class WeatherEmbedHelper
    {
        public static EmbedBuilder GenerateEmbed_CurrentForecast(WeatherResponse result, MeasurementSystem syst)
        {
            // we can ignore all null checks since we check for any invalid WeatherResponse
            // in WHelper.ValidateResponse_Current.
            var embed = new EmbedBuilder()
            {
                Title = $"Weather in {result.Location.Name}, {result.Location.Region}",
                Url = $"https://google.com/maps/@{result.Location.Longitude},{result.Location.Latitude}",
                ThumbnailUrl = "https://" + result.Current.Condition?.IconUrl.Split("//")[1],
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

            return embed;
        }
        public static EmbedBuilder GenerateEmbed_Forecast(WeatherResponse result, MeasurementSystem syst)
        {
            var embed = new EmbedBuilder()
            {
                Title = $"Forecast for {result.Location.Name}, {result.Location.Region}",
                Url = $"https://google.com/maps/@{result.Location.Longitude},{result.Location.Latitude}",
                Color = Color.Blue
            }.WithCurrentTimestamp();

            var fields = new List<(string, string)>();

            foreach (var item in result.Forecast.ForecastDay)
            {
                fields.Add(($"{item.Date.Year}-{item.Date.Month}-{item.Date.Day} ",
                    string.Join("\n", new string[]
                    {
                        $"High: " + (syst == MeasurementSystem.Metric ? $"{item.Day.TemperatureMaximumCelcius}°C" : $"{item.Day.TemperatureMaximumFahrenheit}°F"),
                        $"Low: "   + (syst == MeasurementSystem.Metric ? $"{item.Day.TemperatureMinimumCelcius}°C" : $"{item.Day.TemperatureMinimumFahrenheit}°F"),
                        $"Rain Chance: {item.Day.ChanceOfRain}%"
                    })));
            }

            foreach (var (name, value) in fields)
            {
                embed.AddField(name, value, true);
            }

            return embed;
        }
        public static async Task<EmbedBuilder> CurrentForecast(string location, MeasurementSystem syst)
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
                return embed;
            }

            WeatherResponse? result = null;
            try
            {
                result = await controller.FetchCurrent(location);
            }
            catch (Exception ex)
            {
                embed.Description = $"Exception occurred; `{ex.Message}`";
                return embed;
            }

            var validateResponse = WHelper.ValidateResponse_Current(result);

            // when success is true, result will never be null.
            // any result null errors from now on can be ignored.
            if (!validateResponse.Success)
            {
                embed.Description = validateResponse.Message;
                return embed;
            }

            return GenerateEmbed_CurrentForecast(result, syst);
        }
        public static async Task<EmbedBuilder> Forecast(string location, MeasurementSystem syst)
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
                return embed;
            }

            WeatherResponse? result = null;
            try
            {
                result = await controller.FetchForecast(location, 3);
            }
            catch (Exception ex)
            {
                embed.Description = $"Exception occurred; `{ex.Message}`";
                return embed;
            }

            var validateResponse = WHelper.ValidateResponse_Forecast(result);

            // when success is true, result will never be null.
            // any result null errors from now on can be ignored.
            if (!validateResponse.Success)
            {
                embed.Description = validateResponse.Message;
                return embed;
            }

            return GenerateEmbed_Forecast(result, syst);
        }
    }
}
