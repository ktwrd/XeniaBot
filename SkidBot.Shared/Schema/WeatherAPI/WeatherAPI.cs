using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SkidBot.Shared.Schema.WeatherAPI
{
    public class WeatherResponse
    {
        [JsonPropertyName("location")]
        public WeatherLocation? Location = null;
        [JsonPropertyName("current")]
        public ForecastCurrent? Current = null;
        [JsonPropertyName("forecast")]
        public ForecastParent? Forecast = null;
        [JsonPropertyName("alerts")]
        public WeatherAlert? Alert = null;

        [JsonPropertyName("astronomy")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public AstronomyParent? AstronomyValue = null;

        public ForecastAstrology? Astronomy => AstronomyValue?.Data;
        [JsonPropertyName("error")]
        public WeatherError? Error = null;
    }
    public class WeatherError
    {
        [JsonPropertyName("code")]
        public int Code;
        [JsonPropertyName("message")]
        public string Message = "";
    }

    public enum USEPAIndex
    {
        /// <summary>
        /// Only used when unable to parse <see cref="WeatherAirQuality.EPAValue"/> to <see cref="WeatherAirQuality.EPA"/> value.
        /// </summary>
        Unknown = -1,
        Good = 1,
        Moderate = 2,
        Unhealthy_ForSensitive = 3,
        Unhealthy = 4,
        VeryUnhealthy = 5,
        Hazardous = 6
    }
    public enum UKDefraBand
    {
        /// <summary>
        /// Only used when unable to parse <see cref="WeatherAirQuality.DefraValue"/> to <see cref="WeatherAirQuality.DefraBand"/>
        /// </summary>
        Unknown = -1,
        /// <summary>
        /// When index 1 to 3
        /// </summary>
        Low,
        /// <summary>
        /// When index is 4-6
        /// </summary>
        Moderate,
        /// <summary>
        /// When index is 7-9
        /// </summary>
        High,
        /// <summary>
        /// When index is >= 10
        /// </summary>
        VeryHigh
    }

    public class WeatherCondition
    {
        [JsonPropertyName("text")]
        public string Text;
        [JsonPropertyName("icon")]
        public string IconUrl;
        [JsonPropertyName("code")]
        public int Code;
    }
    public class WeatherLocation
    {
        [JsonPropertyName("name")]
        public string Name;
        [JsonPropertyName("region")]
        public string Region;
        [JsonPropertyName("country")]
        public string Country;
        [JsonPropertyName("lat")]
        public double Latitude;
        [JsonPropertyName("lon")]
        public double Longitude;
        [JsonPropertyName("tz_id")]
        public string TimezoneId;
        [JsonPropertyName("localtime_epoch")]
        public long LocalTimestampEpoch;
        [JsonPropertyName("localtime")]
        public string LocalTimestamp;
    }
}
