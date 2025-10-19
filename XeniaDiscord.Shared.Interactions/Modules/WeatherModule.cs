using Discord;
using Discord.Interactions;
using XeniaBot.Shared.Helpers;
using XeniaBot.Shared.Services;
using XeniaDiscord.Shared.Interactions.Handlers;
using XeniaDiscord.Common.Services;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace XeniaDiscord.Shared.Interactions.Modules;

[Group("weather", "Get info about the weather")]
public class WeatherModule : InteractionModuleBase
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly WeatherModuleService _service;
    private readonly ErrorReportService _err;
    public WeatherModule(IServiceProvider services)
    {
        _service = services.GetRequiredService<WeatherModuleService>();
        _err = services.GetRequiredService<ErrorReportService>();
    }
    [SlashCommand("get", "Fetch weather")]
    public async Task Fetch([Summary("weather_location"), Autocomplete(typeof(WeatherApiAutocompleteHandler))] string location,
        [Summary("system", description: "Measurement system to fetch the weather in.")]
        MeasurementSystem syst)
    {
        await DeferAsync();

        try
        {
            var resultEmbed = await _service.GetCurrentWeatherEmbed(location, syst);
            await Context.Interaction.FollowupAsync(embed: resultEmbed.Build(),
                components: _service.WeatherCurrentComponents(location, syst).Build());
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to fetch weather details for {location}");
            await FollowupAsync(
                embed: new EmbedBuilder()
                    .WithTitle("Weather - Today")
                    .WithDescription($"Failed to fetch data. \n```\n{ex.Message}\n```")
                    .WithColor(Color.Red)
                    .Build());
            await _err.ReportError(ex, Context);
        }
    }

    [SlashCommand("forecast", "Fetch 3 day weather forecast")]
    public async Task Forecast([Summary("weather_location"), Autocomplete(typeof(WeatherApiAutocompleteHandler))] string location,
        [Summary("system", description: "Measurement system to fetch the weather in.")]
        MeasurementSystem syst)
    {
        await DeferAsync();
        try
        {
            var resultEmbed = await _service.GetForecastEmbed(location, syst);
            await Context.Interaction.FollowupAsync(embed: resultEmbed.Build(),
                components: _service.WeatherForecastComponents(location, syst).Build());
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to fetch weather forecast for {location}");
            await FollowupAsync(
                embed: new EmbedBuilder()
                    .WithTitle("Weather - Forecast")
                    .WithDescription($"Failed to fetch data. \n```\n{ex.Message}\n```")
                    .WithColor(Color.Red)
                    .Build());
            await _err.ReportError(ex, Context);
        }
    }
}
