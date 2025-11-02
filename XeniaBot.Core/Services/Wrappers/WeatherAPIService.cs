using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Shared;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using XeniaBot.Shared.Schema;
using System.Text.Json;
using XeniaBot.Shared.Schema.WeatherAPI;
using System.Diagnostics;
using System.Text.Json.Serialization;
using NLog;

namespace XeniaBot.Core.Services.Wrappers;

[XeniaController]
public class WeatherAPIService : BaseService
{
    private readonly Logger _log = LogManager.GetLogger("Xenia." + nameof(WeatherAPIService));
    protected ConfigData _sysconfig;
    public WeatherAPIService(IServiceProvider services)
        : base(services)
    {
        serializerOptions = new JsonSerializerOptions()
        {
            IgnoreReadOnlyFields = true,
            IgnoreReadOnlyProperties = true,
            IncludeFields = true,
            WriteIndented = true,
            ReferenceHandler = ReferenceHandler.Preserve
        };

        _sysconfig = services.GetRequiredService<ConfigData>();
        if (_sysconfig == null)
        {
            _log.Error("Service \"ConfigService.Config\" is null!!!");
            Program.Quit();
            return;
        }


        _log.Debug("Validating WeatherAPI.com API Key");
        Enable = _sysconfig.ApiKeys.Weather != null && ValidateToken(_sysconfig.ApiKeys.Weather);
    }
    private bool Enable = true;
    protected HttpClient HttpClient = new HttpClient();
    public JsonSerializerOptions serializerOptions;

    protected bool ValidateToken(string token)
    {
        if (token.Length < 0)
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
            _log.Debug($"false: Caught Exception {ex.Message}");
            Debugger.Break();
            return false;
        }
        if (response == null)
        {
            _log.Debug($"false: null response");
            return false;
        }

        if (response.Error != null)
        {
            _log.Error($"Failed to validate token. \"{response.Error.Code}: {response.Error.Message}\"");
            return false;
        }

        return true;
    }

    public async Task<WeatherResponse?> FetchCurrent(string location, bool airQuality=true)
    {
        if (!Enable)
        {
            _log.Error("Cannot run function, controller disabled");
            throw new Exception("Client failed validation when init. Aborting");
        }
        _log.Debug($"Query {location} (airQuality: {airQuality})");
        var url = WeatherAPIEndpoint.Current(_sysconfig.ApiKeys.Weather, location, airQuality);
        var response = await HttpClient.GetAsync(url);
        int statusCode = (int)response.StatusCode;
        bool deser = WeatherAPIEndpoint.StatusCodeError.Contains(statusCode) || WeatherAPIEndpoint.StatusCodeSuccess.Contains(statusCode);
        var stringContent = response.Content.ReadAsStringAsync().Result;
        if (!deser)
        {
            throw new InvalidOperationException($"Invalid status code of {statusCode} for request to {url} with response: {stringContent}");
        }

        var content = JsonSerializer.Deserialize<WeatherResponse>(stringContent, serializerOptions);
        return content;
    }
    public async Task<WeatherSearchResponse> SearchAutocomplete(string query)
    {
        if (!Enable)
        {
            _log.Error("Cannot run function, controller disabled");
            throw new Exception("Client failed validation when init. Aborting");
        }
        _log.Debug($"Requesting autocomplete for \"{query}\"");
        string url = WeatherAPIEndpoint.Search(_sysconfig.ApiKeys.Weather, query);
        var response = await HttpClient.GetAsync(url);
        int statusCode = (int)response.StatusCode;
        bool errorDeser = WeatherAPIEndpoint.StatusCodeError.Contains(statusCode);
        bool succDeser = WeatherAPIEndpoint.StatusCodeSuccess.Contains(statusCode);
        var stringContent = response.Content.ReadAsStringAsync().Result;
        if (!errorDeser && !succDeser)
        {
            throw new InvalidOperationException($"Invalid status code of {statusCode} for request to {url} with response: {stringContent}");
        }

        var instance = new WeatherSearchResponse();
        if (errorDeser)
        {
            instance.Error = JsonSerializer.Deserialize<WeatherError>(stringContent, serializerOptions);
            _log.Debug($"Failed query \"{query}\", {instance.Error?.Code -1} \"{instance.Error?.Message}\"");
        }
        if (succDeser)
        {
            instance.Response = JsonSerializer.Deserialize<WeatherSearchItem[]>(stringContent, serializerOptions)
                ?? Array.Empty<WeatherSearchItem>();
            _log.Debug($"Query \"{query}\" yielded {instance.Response.Length} items");
        }

        return instance;
    }

    public async Task<WeatherResponse?> FetchForecast(string location, int days = 7, bool airQuality = true, bool alerts = true)
    {
        if (!Enable)
        {
            _log.Error("Cannot run function, controller disabled");
            throw new Exception("Client failed validation when init. Aborting");
        }
#if DEBUG
        _log.Debug($"Requesting {days}d forecast for \"{location}\". (airQuality: {airQuality}, alerts: {alerts})");
#endif
        if (days < 1 || days > 10)
            throw new ArgumentException("Argument \"days\" must be >= 1 or <= 10");

        var url = WeatherAPIEndpoint.Forecast(_sysconfig.ApiKeys.Weather, location, days, airQuality, alerts);
        var response = await HttpClient.GetAsync(url);

        int statusCode = (int)response.StatusCode;
        bool deser = WeatherAPIEndpoint.StatusCodeError.Contains(statusCode) || WeatherAPIEndpoint.StatusCodeSuccess.Contains(statusCode);
        var stringContent = response.Content.ReadAsStringAsync().Result;
        if (!deser)
        {
            _log.Error($"Failed to fetch {url}, invalid status code {statusCode}.\n{stringContent}");
#if DEBUG
            Debugger.Break();
#endif
            throw new InvalidOperationException($"Invalid status code of {statusCode} for request to {url} with response: {stringContent}");
        }

        var content = JsonSerializer.Deserialize<WeatherResponse>(stringContent, serializerOptions);
        return content;
    }
}
