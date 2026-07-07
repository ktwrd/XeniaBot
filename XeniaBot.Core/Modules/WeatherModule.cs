using Discord;
using Discord.Interactions;
using JetBrains.Annotations;
using NLog;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Core.Services.BotAdditions;
using XeniaBot.Core.Services.Wrappers;
using XeniaBot.Shared;
using XeniaBot.Shared.Helpers;
using XeniaBot.Shared.Services;

namespace XeniaBot.Core.Modules;

[Group("weather", "Get info about the weather")]
public class WeatherModule : InteractionModuleBase
{
    private static readonly Logger Log = LogManager.GetLogger("Xenia.Interaction." + nameof(WeatherModule));

    private readonly WeatherModuleService _service;
    private readonly ErrorReportService _err;

    public WeatherModule(IServiceProvider services)
    {
        _service = services.GetRequiredService<WeatherModuleService>();
        _err = services.GetRequiredService<ErrorReportService>();
    }
    
    [SlashCommand("get", "Fetch weather")]
    [RegisterDBLCommand]
    [UsedImplicitly]
    public async Task Fetch(
        [Summary("weather_location")]
        [Autocomplete(typeof(WeatherAPIAutocompleteHandler))]
        string location,
        [Summary("system", description: "Measurement system to fetch the weather in.")]
        MeasurementSystem syst)
    {
        await DeferAsync();

        try
        {
            var resultEmbed = await _service.GetCurrentWeatherEmbed(location, syst);
            await FollowupAsync(embed: resultEmbed.Build(),
                components: _service.WeatherCurrentComponents(location, syst).Build());
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Failed to fetch weather details for {location}");
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
    [RegisterDBLCommand]
    public async Task Forecast(
        [Summary("weather_location")]
        [Autocomplete(typeof(WeatherAPIAutocompleteHandler))]
        string location,
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
            Log.Error($"Failed to fetch weather forecast for {location}.\n{ex}");
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
