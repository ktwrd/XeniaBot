using Discord;
using XeniaBot.Shared.Helpers;

namespace XeniaDiscord.Common.Interfaces;

public interface IWeatherModuleService
{
    public Task<EmbedBuilder> GetForecastEmbed(string location, MeasurementSystem system);
    public ComponentBuilder WeatherCurrentComponents(string location, MeasurementSystem system);
    public ComponentBuilder WeatherForecastComponents(string location, MeasurementSystem system);
    public Task<EmbedBuilder> GetCurrentWeatherEmbed(string location, MeasurementSystem system);
}
