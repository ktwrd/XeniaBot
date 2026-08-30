using XeniaBot.Shared.Schema.WeatherAPI;

namespace XeniaBot.Shared.Helpers
{
    public class ValidateResponseResult
    {
        /// <summary>
        /// When true, then the result given is perfectly fine. The result parameter in ValidateResponse will always be not-null when true.
        /// </summary>
        public bool Success { get; set; }
        public string Message { get; set; }
        /// <summary>
        /// Populated when <see cref="WeatherResponse.Error"/> is not null.
        /// </summary>
        public int ErrorCode { get; set; }
        public override string ToString()
        {
            return Message;
        }
        public ValidateResponseResult(bool success, string message = "", int errorCode = 0)
        {
            Success = success;
            Message = message;
            ErrorCode = errorCode;
        }
    }
    public partial class WeatherHelper
    {
        public static string FetchErrorDescription(WeatherError error)
        {
            var content = $"`{error.Code}` ";
            content += error.Code switch
            {
                1003 or 1006 => $"Location not found ({error.Code})",
                1008 => $"API key cannot fetch historical data.",
                2006 => $"API Key is invalid",
                2009 => $"API key does not have access to this resource.",
                9999 => $"Internal application error from weatherapi.com",
                _ => error.Message
            };
            return content;
        }
        /// <summary>
        /// Validate the response given by weatherapi.com for any errors.
        /// </summary>
        public static ValidateResponseResult ValidateResponse(
            WeatherResponse? result,
            bool matchCurrent = false,
            bool matchLocation = true,
            bool matchForecast = false,
            bool matchAlerts = false,
            bool matchAstro = false)
        {
            if (result == null)
            {
                return new ValidateResponseResult(false, "Invalid response from server (null)");
            }
            else if (result.Error != null)
            {
                return new ValidateResponseResult(false, FetchErrorDescription(result.Error), result.Error.Code);
            }
            else if (result.Current == null && matchCurrent)
            {
                return new ValidateResponseResult(false, "Current weather data missing");
            }
            else if (result.Location == null && matchLocation)
            {
                return new ValidateResponseResult(false, "Location data missing");
            }
            else if (result.Forecast == null && matchForecast)
            {
                return new ValidateResponseResult(false, "Forecast data missing");
            }
            else if (result.Alert == null && matchAlerts)
            {
                return new ValidateResponseResult(false, "Weather Alerts missing");
            }
            else if (result.Astronomy == null && matchAstro)
            {
                return new ValidateResponseResult(false, "Astronomy data missing");
            }
            return new ValidateResponseResult(true);
        }
        public static ValidateResponseResult ValidateResponse_Current(WeatherResponse? result)
            => ValidateResponse(result, true, true, false);
        public static ValidateResponseResult ValidateResponse_Forecast(WeatherResponse? result, bool matchAlerts = false)
            => ValidateResponse(result, false, true, true, matchAlerts: matchAlerts);
        public static ValidateResponseResult ValidateResponse_Astro(WeatherResponse? result)
            => ValidateResponse(result, false, false, false, false, true);
    }
}
