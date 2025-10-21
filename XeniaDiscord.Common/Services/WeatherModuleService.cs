using Discord;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Shared.Helpers;
using XeniaBot.Shared.Schema.WeatherAPI;
using XeniaDiscord.Common.Interfaces;

namespace XeniaDiscord.Common.Services;

/// <summary>
/// Used to provide embeds and for handling buttons for <see cref="Modules.WeatherModule"/>
/// </summary>
public class WeatherModuleService : IWeatherModuleService
{
    private readonly WeatherApiService _weather;

    public WeatherModuleService(IServiceProvider services)
    {
        _weather = services.GetRequiredService<WeatherApiService>();
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
            embed.WithDescription($"Failed to fetch data (`{ex.GetType().Namespace}.{ex.GetType().Name}`)")
                 .AddField("Message", ex.Message[..2000], false)
                 .WithColor(Color.Red);
            return embed;
        }

        var validateResponse = WeatherHelper.ValidateResponse_Forecast(result);

        // when success is true, result will never be null.
        // any result null errors from now on can be ignored.
        if (!validateResponse.Success || result == null)
        {
            embed.Description = validateResponse.Message;
            return embed;
        }
        embed = new EmbedBuilder()
        {
            Title = $"Forecast for {result.Location?.Name}, {result.Location?.Region}",
            Color = Color.Blue
        }.WithCurrentTimestamp();
        if (result.Location != null)
        {
            embed.Url = $"https://google.com/maps/@{result.Location.Longitude},{result.Location.Latitude}";
        }

        var fields = new List<(string, string)>();

        foreach (var item in result.Forecast?.ForecastDay ?? [])
        {
            if (item.Day == null) continue;
            fields.Add(($"{item.Date.Year}-{item.Date.Month}-{item.Date.Day} ",
                string.Join("\n",
                    $"High: " + (system == MeasurementSystem.Metric ? $"{item.Day.TemperatureMaximumCelcius}°C" : $"{item.Day.TemperatureMaximumFahrenheit}°F"),
                    $"Low: "   + (system == MeasurementSystem.Metric ? $"{item.Day.TemperatureMinimumCelcius}°C" : $"{item.Day.TemperatureMinimumFahrenheit}°F"),
                    $"Rain Chance: {item.Day.ChanceOfRain}%"
                )));
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
                .WithButton("Refresh", InteractionIdentifier.WeatherTodayRefresh + $"\n{location}\n{(int)system}"));
        return builder;
    }

    public ComponentBuilder WeatherForecastComponents(string location, MeasurementSystem system)
    {
        var builder = new ComponentBuilder();
        builder.AddRow(
            new ActionRowBuilder()
                .WithButton("Refresh", InteractionIdentifier.WeatherForecastRefresh + $"\n{location}\n{(int)system}"));
        return builder;
    }
    public async Task<EmbedBuilder> GetCurrentWeatherEmbed(string location, MeasurementSystem system)
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