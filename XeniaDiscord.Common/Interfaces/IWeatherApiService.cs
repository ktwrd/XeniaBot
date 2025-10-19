using XeniaBot.Shared.Schema.WeatherAPI;

namespace XeniaDiscord.Common.Interfaces;

public interface IWeatherApiService
{
    public Task<WeatherResponse?> FetchCurrent(string location, bool airQuality = true);
    public Task<WeatherSearchResponse> SearchAutocomplete(string query);
    public Task<WeatherResponse?> FetchForecast(string location, int days = 7, bool airQuality = true, bool alerts = true);
}
