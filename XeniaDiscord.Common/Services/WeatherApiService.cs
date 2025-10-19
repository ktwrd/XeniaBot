using Microsoft.Extensions.DependencyInjection;
using NLog;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using XeniaBot.Shared.Config;
using XeniaBot.Shared.Schema;
using XeniaBot.Shared.Schema.WeatherAPI;
using XeniaDiscord.Common.Interfaces;

namespace XeniaDiscord.Common.Services;

public class WeatherApiService : IWeatherApiService
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly XeniaConfig _config;
    public WeatherApiService(IServiceProvider services)
    {
        _config = services.GetRequiredService<XeniaConfig>();
        _log.Debug("Validating WeatherAPI.com API Key");
        _enabled = !string.IsNullOrEmpty(_config.ApiKey.Weather?.Trim()) && ValidateToken(_config.ApiKey.Weather);
    }
    private readonly bool _enabled = true;
    private readonly HttpClient _client = new();
    private static JsonSerializerOptions SerializerOptions => new()
    {
        IgnoreReadOnlyFields = true,
        IgnoreReadOnlyProperties = true,
        IncludeFields = true,
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.Preserve
    };

    protected bool ValidateToken(string token)
    {
        if (string.IsNullOrEmpty(token.Trim()))
        {
            _log.Debug("false: too short");
            return false;
        }

        WeatherResponse? response = null;
        try
        {
            response = FetchCurrent("Perth, Western Australia", false).Result;
        }
        catch (Exception ex)
        {
            _log.Debug(ex, "false: Caught Exception");
            Debugger.Break();
            return false;
        }
        if (response == null)
        {
            _log.Debug("false: null response");
            return false;
        }

        if (response.Error != null)
        {
            _log.Error($"Failed to validate token. \"{response.Error.Code}: {response.Error.Message}\"");
            return false;
        }

        return true;
    }

    public async Task<WeatherResponse?> FetchCurrent(string location, bool airQuality = true)
    {
        if (!_enabled)
        {
            _log.Warn("Cannot run function, controller disabled");
            return null;
        }
        _log.Debug($"Query {location} (airQuality: {airQuality})");
        var url = WeatherAPIEndpoint.Current(_config.ApiKey.Weather!, location, airQuality);
        var response = await _client.GetAsync(url);
        int statusCode = (int)response.StatusCode;
        bool deser = WeatherAPIEndpoint.StatusCodeError.Contains(statusCode) || WeatherAPIEndpoint.StatusCodeSuccess.Contains(statusCode);
        var stringContent = response.Content.ReadAsStringAsync().Result;
        if (!deser)
        {
            _log.Error($"Failed to fetch {url}, invalid status code {statusCode}.\n{stringContent}");
            throw new InvalidOperationException($"Invalid status code of {statusCode}");
        }

        var content = JsonSerializer.Deserialize<WeatherResponse>(stringContent, SerializerOptions);
        return content;
    }
    public async Task<WeatherSearchResponse> SearchAutocomplete(string query)
    {
        if (!_enabled)
        {
            _log.Warn("Cannot run function, controller disabled");
            throw new InvalidOperationException("Client failed validation when init. Aborting");
        }
        _log.Debug($"Requesting autocomplete for \"{query}\"");
        string url = WeatherAPIEndpoint.Search(_config.ApiKey.Weather!, query);
        var response = await _client.GetAsync(url);
        int statusCode = (int)response.StatusCode;
        bool errorDeser = WeatherAPIEndpoint.StatusCodeError.Contains(statusCode);
        bool succDeser = WeatherAPIEndpoint.StatusCodeSuccess.Contains(statusCode);
        var stringContent = response.Content.ReadAsStringAsync().Result;
        if (!errorDeser && !succDeser)
        {
            _log.Error($"Failed to fetch {url}, invalid status code {statusCode}.\n{stringContent}");
            throw new InvalidOperationException($"Invalid status code of {statusCode}");
        }

        var instance = new WeatherSearchResponse();
        if (errorDeser)
        {
            instance.Error = JsonSerializer.Deserialize<WeatherError>(stringContent, SerializerOptions);
            _log.Debug($"Failed query \"{query}\", {instance.Error?.Code - 1} \"{instance.Error?.Message}\"");
        }
        if (succDeser)
        {
            instance.Response = JsonSerializer.Deserialize<WeatherSearchItem[]>(stringContent, SerializerOptions) ?? [];
            _log.Debug($"Query \"{query}\" yielded {instance.Response.Length} items");
        }

        return instance;
    }

    public async Task<WeatherResponse?> FetchForecast(string location, int days = 7, bool airQuality = true, bool alerts = true)
    {
        if (!_enabled)
        {
            _log.Warn("Cannot run function, controller disabled");
            return null;
        }
        _log.Debug($"Requesting {days}d forecast for \"{location}\". (airQuality: {airQuality}, alerts: {alerts})");
        if (days < 1 || days > 10)
            throw new ArgumentException($"Value must be >= 1 or <= 10 (got: {days})", nameof(days));

        var url = WeatherAPIEndpoint.Forecast(_config.ApiKey.Weather!, location, days, airQuality, alerts);
        var response = await _client.GetAsync(url);

        int statusCode = (int)response.StatusCode;
        bool deser = WeatherAPIEndpoint.StatusCodeError.Contains(statusCode) || WeatherAPIEndpoint.StatusCodeSuccess.Contains(statusCode);
        var stringContent = response.Content.ReadAsStringAsync().Result;
        if (!deser)
        {
            _log.Error($"Failed to fetch {url}, invalid status code {statusCode}.\n{stringContent}");
            throw new InvalidOperationException($"Invalid status code of {statusCode}");
        }

        var content = JsonSerializer.Deserialize<WeatherResponse>(stringContent, SerializerOptions);
        return content;
    }
}
