using Discord;
using Discord.Interactions;
using XeniaBot.Core.Services.Wrappers;
using XeniaBot.Shared.Helpers;
using System;
using System.Threading.Tasks;
using XeniaBot.Core.Services.BotAdditions;
using XeniaBot.Shared;
using XeniaBot.Shared.Services;

namespace XeniaBot.Core.Modules
{
    [Group("weather", "Get info about the weather")]
    public class WeatherModule : InteractionModuleBase
    {
        [SlashCommand("get", "Fetch weather")]
        public async Task Fetch([Summary("weather_location"), Autocomplete(typeof(WeatherAPIAutocompleteHandler))] string location,
            [Summary("system", description: "Measurement system to fetch the weather in.")]
            MeasurementSystem syst)
        {
            await DeferAsync();
            var controller = CoreContext.Instance?.GetRequiredService<WeatherModuleService>();
            if (controller == null)
            {
                await FollowupAsync(
                    embed: new EmbedBuilder()
                        .WithTitle("Weather - Today")
                        .WithDescription($"Failed to get WeatherModuleService")
                        .WithColor(Color.Red)
                        .Build());
                return;
            }

            try
            {
                var resultEmbed = await controller.GetCurrentWeatherEmbed(location, syst);
                await Context.Interaction.FollowupAsync(embed: resultEmbed.Build(),
                    components: controller.WeatherCurrentComponents(location, syst).Build());
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to fetch weather details for {location}.\n{ex}");
                await FollowupAsync(
                    embed: new EmbedBuilder()
                        .WithTitle("Weather - Today")
                        .WithDescription($"Failed to fetch data. \n```\n{ex.Message}\n```")
                        .WithColor(Color.Red)
                        .Build());
                var errorService = CoreContext.Instance?.GetRequiredService<ErrorReportService>();
                if (errorService != null)
                {
                    await errorService.ReportError(ex, Context);
                }
            }
        }

        [SlashCommand("forecast", "Fetch 3 day weather forecast")]
        public async Task Forecast([Summary("weather_location"), Autocomplete(typeof(WeatherAPIAutocompleteHandler))] string location,
            [Summary("system", description: "Measurement system to fetch the weather in.")]
            MeasurementSystem syst)
        {
            await DeferAsync();
            var controller = CoreContext.Instance?.GetRequiredService<WeatherModuleService>();
            if (controller == null)
            {
                await FollowupAsync(
                    embed: new EmbedBuilder()
                        .WithTitle("Weather - Forecast")
                        .WithDescription($"Failed to get WeatherModuleService")
                        .WithColor(Color.Red)
                        .Build());
                return;
            }

            try
            {
                var resultEmbed = await controller.GetForecastEmbed(location, syst);
                await Context.Interaction.FollowupAsync(embed: resultEmbed.Build(),
                    components: controller.WeatherForecastComponents(location, syst).Build());
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
                var errorService = CoreContext.Instance?.GetRequiredService<ErrorReportService>();
                if (errorService != null)
                {
                    await errorService.ReportError(ex, Context);
                }
            }
        }
    }
}
