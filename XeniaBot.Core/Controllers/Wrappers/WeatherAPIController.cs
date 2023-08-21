using Microsoft.Extensions.DependencyInjection;
using XeniaBot.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using XeniaBot.Shared.Schema;
using System.Text.Json;
using XeniaBot.Shared.Schema.WeatherAPI;
using System.Diagnostics;

namespace XeniaBot.Core.Controllers.Wrappers
{
    [BotController]
    public class WeatherAPIController : BaseController
    {
        protected ConfigData _sysconfig;
        public WeatherAPIController(IServiceProvider services)
            : base(services)
        {
            serializerOptions = new JsonSerializerOptions()
            {
                IgnoreReadOnlyFields = true,
                IgnoreReadOnlyProperties = true,
                IncludeFields = true,
                WriteIndented = true
            };

            _sysconfig = services.GetRequiredService<ConfigData>();
            if (_sysconfig == null)
            {
                Log.Error("Service \"ConfigController.Config\" is null!!!");
                Program.Quit();
                return;
            }


            Log.Debug("Validating WeatherAPI.com API Key");
            Enable = ValidateToken(_sysconfig.WeatherAPI_Key);
        }
        private bool Enable = true;
        protected HttpClient HttpClient = new HttpClient();
        public JsonSerializerOptions serializerOptions;

        protected bool ValidateToken(string token)
        {
            if (token.Length < 0)
            {
                Log.Debug($"false: too short");
                return false;
            }

            WeatherResponse? response = null;
            try
            {
                response = FetchCurrent("Perth, Western Australia", false).Result;
            }
            catch (Exception ex)
            {
                Log.Debug($"false: Caught Exception {ex.Message}");
                Debugger.Break();
                return false;
            }
            if (response == null)
            {
                Log.Debug($"false: null response");
                return false;
            }

            if (response.Error != null)
            {
                Log.Error($"Failed to validate token. \"{response.Error.Code}: {response.Error.Message}\"");
                return false;
            }

            return true;
        }

        public async Task<WeatherResponse?> FetchCurrent(string location, bool airQuality=true)
        {
            if (!Enable)
            {
                Log.Error("Cannot run function, controller disabled");
                throw new Exception("Client failed validation when init. Aborting");
            }
#if DEBUG
            Log.Debug($"Query {location} (airQuality: {airQuality})");
#endif
            var url = WeatherAPIEndpoint.Current(_sysconfig.WeatherAPI_Key, location, airQuality);
            var response = await HttpClient.GetAsync(url);
            int statusCode = (int)response.StatusCode;
            bool deser = WeatherAPIEndpoint.StatusCodeError.Contains(statusCode) || WeatherAPIEndpoint.StatusCodeSuccess.Contains(statusCode);
            var stringContent = response.Content.ReadAsStringAsync().Result;
            if (!deser)
            {
                Log.Error($"Failed to fetch {url}, invalid status code {statusCode}.\n{stringContent}");
#if DEBUG
                Debugger.Break();
#endif
                throw new Exception($"Invalid status code of {statusCode}");
            }

            var content = JsonSerializer.Deserialize<WeatherResponse>(stringContent, serializerOptions);
            return content;
        }
        public async Task<WeatherSearchResponse> SearchAutocomplete(string query)
        {
            if (!Enable)
            {
                Log.Error("Cannot run function, controller disabled");
                throw new Exception("Client failed validation when init. Aborting");
            }
#if DEBUG
            Log.Debug($"Requesting autocomplete for \"{query}\"");
#endif
            string url = WeatherAPIEndpoint.Search(_sysconfig.WeatherAPI_Key, query);
            var response = await HttpClient.GetAsync(url);
            int statusCode = (int)response.StatusCode;
            bool errorDeser = WeatherAPIEndpoint.StatusCodeError.Contains(statusCode);
            bool succDeser = WeatherAPIEndpoint.StatusCodeSuccess.Contains(statusCode);
            var stringContent = response.Content.ReadAsStringAsync().Result;
            if (errorDeser == false && succDeser == false)
            {
                Log.Error($"Failed to fetch {url}, invalid status code {statusCode}.\n{stringContent}");
                throw new Exception($"Invalid status code of {statusCode}");
            }

            var instance = new WeatherSearchResponse();
            if (errorDeser)
            {
                instance.Error = JsonSerializer.Deserialize<WeatherError>(stringContent, serializerOptions);
                Log.Debug($"Failed query \"{query}\", {instance.Error?.Code -1} \"{instance.Error?.Message}\"");
            }
            if (succDeser)
            {
                instance.Response = JsonSerializer.Deserialize<WeatherSearchItem[]>(stringContent, serializerOptions)
                    ?? Array.Empty<WeatherSearchItem>();
                Log.Debug($"Query \"{query}\" yielded {instance.Response.Length} items");
            }

            return instance;
        }
    
        public async Task<WeatherResponse?> FetchForecast(string location, int days = 7, bool airQuality = true, bool alerts = true)
        {
            if (!Enable)
            {
                Log.Error("Cannot run function, controller disabled");
                throw new Exception("Client failed validation when init. Aborting");
            }
#if DEBUG
            Log.Debug($"Requesting {days}d forecast for \"{location}\". (airQuality: {airQuality}, alerts: {alerts})");
#endif
            if (days < 1 || days > 10)
                throw new ArgumentException("Argument \"days\" must be >= 1 or <= 10");

            var url = WeatherAPIEndpoint.Forecast(_sysconfig.WeatherAPI_Key, location, days, airQuality, alerts);
            var response = await HttpClient.GetAsync(url);

            int statusCode = (int)response.StatusCode;
            bool deser = WeatherAPIEndpoint.StatusCodeError.Contains(statusCode) || WeatherAPIEndpoint.StatusCodeSuccess.Contains(statusCode);
            var stringContent = response.Content.ReadAsStringAsync().Result;
            if (!deser)
            {
                Log.Error($"Failed to fetch {url}, invalid status code {statusCode}.\n{stringContent}");
#if DEBUG
                Debugger.Break();
#endif
                throw new Exception($"Invalid status code of {statusCode}");
            }

            var content = JsonSerializer.Deserialize<WeatherResponse>(stringContent, serializerOptions);
            return content;
        }
    }
}
