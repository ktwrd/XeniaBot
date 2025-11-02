using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using XeniaBot.Core.Services.Wrappers;
using XeniaBot.Shared;
using XeniaBot.Shared.Helpers;
using XeniaBot.Shared.Schema.WeatherAPI;
using XeniaBot.Shared.Services;

namespace XeniaBot.Core.Services.BotAdditions;

/// <summary>
/// Used to provide embeds and for handling buttons for <see cref="XeniaBot.Core.Modules.WeatherModule"/>
/// </summary>
[XeniaController]
public class WeatherModuleService : BaseService
{
    private readonly Logger _log = LogManager.GetLogger("Xenia." + nameof(WeatherModuleService));
    private readonly DiscordSocketClient _discord;
    private readonly WeatherAPIService _weather;
    private readonly ErrorReportService _error;

    public WeatherModuleService(IServiceProvider services)
        : base(services)
    {
        _discord = services.GetRequiredService<DiscordSocketClient>();
        _weather = services.GetRequiredService<WeatherAPIService>();
        _error = services.GetRequiredService<ErrorReportService>();
        
        _discord.ButtonExecuted += DiscordOnButtonExecuted;
    }

    private async Task DiscordOnButtonExecuted(SocketMessageComponent arg)
    {
        if (!arg.Data.CustomId.StartsWith("weather-"))
        {
            return;
        }

        try
        {
            var param = arg.Data.CustomId.Split(" ");
            EmbedBuilder? embed = null;
            ComponentBuilder? builder = null;
            if (param[0] == "weather-forecast")
            {
                embed = await GetForecastEmbed(
                    param[1], param[2] == "1" ? MeasurementSystem.Imperial : MeasurementSystem.Metric);
                builder = WeatherForecastComponents(
                    param[1], param[2] == "1" ? MeasurementSystem.Imperial : MeasurementSystem.Metric);
            }
            else if (param[0] == "weather-current")
            {
                embed = await GetCurrentWeatherEmbed(
                    param[1], param[2] == "1" ? MeasurementSystem.Imperial : MeasurementSystem.Metric);
                builder = WeatherCurrentComponents(
                    param[1], param[2] == "1" ? MeasurementSystem.Imperial : MeasurementSystem.Metric);
            }

            if (embed == null)
            {
                await arg.RespondAsync(
                    embed: new EmbedBuilder()
                        .WithTitle("Weather Refresh")
                        .WithDescription($"No handler for action `{param[0]}`")
                        .WithColor(Color.Red).Build());
                return;
            }
            
            await arg.RespondAsync(
                embed: embed.Build(),
                components: builder?.Build());
        }
        catch (Exception ex)
        {
            var msg =
                $"Failed to run button {arg.Data.CustomId} in channel {arg.Message.Channel.Id} for {arg.Message.Author} ({arg.Message.Author.Id})";
            _log.Error(ex, msg);
            await _error.ReportException(ex, msg);
            
            await arg.RespondAsync(
                embed: new EmbedBuilder()
                    .WithTitle("Weather Refresh")
                    .WithDescription($"Failed to fetch weather data.\n```\n{ex.Message}\n```")
                    .WithColor(Color.Red)
                    .Build());
        }
    }

    public async Task<EmbedBuilder> GetForecastEmbed(string location, MeasurementSystem system)
    {
        var embed = new EmbedBuilder()
        {
            Color = Color.Red
        }.WithCurrentTimestamp();
        
        WeatherResponse? result = null;
        try
        {
            result = await _weather.FetchForecast(location, 3);
        }
        catch (Exception ex)
        {
            embed.Description = $"Exception occurred.\n```\n{ex.Message}\n```";
            return embed;
        }

        var validateResponse = WeatherHelper.ValidateResponse_Forecast(result);

        // when success is true, result will never be null.
        // any result null errors from now on can be ignored.
        if (!validateResponse.Success)
        {
            embed.Description = validateResponse.Message;
            return embed;
        }
        embed = new EmbedBuilder()
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
                    $"High: " + (system == MeasurementSystem.Metric ? $"{item.Day.TemperatureMaximumCelcius}°C" : $"{item.Day.TemperatureMaximumFahrenheit}°F"),
                    $"Low: "   + (system == MeasurementSystem.Metric ? $"{item.Day.TemperatureMinimumCelcius}°C" : $"{item.Day.TemperatureMinimumFahrenheit}°F"),
                    $"Rain Chance: {item.Day.ChanceOfRain}%"
                })));
        }

        foreach (var (name, value) in fields)
        {
            embed.AddField(name, value, true);
        }

        return embed;
    }

    public ComponentBuilder WeatherCurrentComponents(string location, MeasurementSystem system)
    {
        var builder = new ComponentBuilder();
        builder.AddRow(
            new ActionRowBuilder()
                .WithButton("Refresh", $"weather-current {location} {(int)system}"));
        return builder;
    }

    public ComponentBuilder WeatherForecastComponents(string location, MeasurementSystem system)
    {
        var builder = new ComponentBuilder();
        builder.AddRow(
            new ActionRowBuilder()
                .WithButton("Refresh", $"weather-forecast {location} {(int)system}"));
        return builder;
    }
    public async Task<EmbedBuilder> GetCurrentWeatherEmbed(String location, MeasurementSystem system)
    {
        var embed = new EmbedBuilder()
        {
            Color = Color.Red
        }.WithCurrentTimestamp();

        WeatherResponse? result = null;
        try
        {
            result = await _weather.FetchCurrent(location);
        }
        catch (Exception ex)
        {
            embed.Description = $"Exception occurred; \n```\n{ex.Message}\n```";
            return embed;
        }

        WeatherResponse? todayResult = null;
        try
        {
            todayResult = await _weather.FetchForecast(location, 1);
        }
        catch (Exception ex)
        {
            embed.Description = $"Exception occurred; \n```\n{ex.Message}\n```";
            return embed;
        }

        var validateResponse = WeatherHelper.ValidateResponse_Current(result);

        // when success is true, result will never be null.
        // any result null errors from now on can be ignored.
        if (!validateResponse.Success)
        {
            embed.Description = validateResponse.Message;
            return embed;
        }

        var forecastValidateResponse = WeatherHelper.ValidateResponse_Forecast(todayResult);

        if (!forecastValidateResponse.Success)
        {
            embed.Description = forecastValidateResponse.Message;
            return embed;
        }

        embed = new EmbedBuilder()
        {
            Title = $"Weather in {result.Location.Name}, {result.Location.Region}",
            Url = $"https://google.com/maps/@{result.Location.Longitude},{result.Location.Latitude}",
            ThumbnailUrl = "https://" + result.Current.Condition?.IconUrl.Split("//")[1],
            Color = Color.Blue
        }.WithCurrentTimestamp();
        string temperature = "";
        temperature += system == MeasurementSystem.Metric
            ? $"{result.Current.TemperatureCelcius}°C"
            : $"{result.Current.TemperatureFahrenheit}°F";
        temperature += "\n Min/Max: ";
        temperature += system == MeasurementSystem.Metric
            ? $"{todayResult?.Forecast?.ForecastDay?.First()?.Day?.TemperatureMinimumCelcius}°C"
            : $"{todayResult?.Forecast?.ForecastDay?.First()?.Day?.TemperatureMinimumFahrenheit}°F";
        temperature += "/";
        temperature += system == MeasurementSystem.Metric
            ? $"{todayResult?.Forecast?.ForecastDay?.First()?.Day?.TemperatureMaximumCelcius}°C"
            : $"{todayResult?.Forecast?.ForecastDay?.First()?.Day?.TemperatureMaximumFahrenheit}°F";
        var fields = new List<(string, string)>()
        {
            ("Temperature", temperature),
            ("Feels Like", system == MeasurementSystem.Metric ? $"{result.Current.TemperatureFeelsLikeCelcius}°C" : $"{result.Current.TemperatureFeelsLikeFahrenheit}°F"),
            ("Wind Speed", system == MeasurementSystem.Metric ? $"{result.Current.WindSpeedKph}KPH" : $"{result.Current.WindSpeedMph}MPH"),
            ("Wind Direction", result.Current.WindDirection),
            ("Humidity", $"{result.Current.Humidity}%"),
            ("Cloud Coverage", $"{result.Current.CloudCoverage}"),
            ("Visibility", system == MeasurementSystem.Metric ? $"{result.Current.VisibilityKm}KM" : $"{result.Current.VisiblityMiles}Mi"),
            ("UV Index", result.Current.UV.ToString()),
            ("Precipitation", system == MeasurementSystem.Metric ? $"{result.Current.PrecipitationMm}mm" : $"{result.Current.PrecipitationIn}in")
        };
        foreach (var (name, value) in fields)
        {
            embed.AddField(name, value, true);
        }

        return embed;
    }
}