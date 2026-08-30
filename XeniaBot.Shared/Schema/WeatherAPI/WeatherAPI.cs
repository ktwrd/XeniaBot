using System.ComponentModel;
using System.Text.Json.Serialization;

namespace XeniaBot.Shared.Schema.WeatherAPI
{
    public class WeatherResponse
    {
        [JsonPropertyName("location")]
        public WeatherLocation? Location { get; set; }
        [JsonPropertyName("current")]
        public ForecastCurrent? Current { get; set; }
        [JsonPropertyName("forecast")]
        public ForecastParent? Forecast { get; set; }
        [JsonPropertyName("alerts")]
        public WeatherAlert? Alert { get; set; }

        [JsonPropertyName("astronomy")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public AstronomyParent? AstronomyValue { get; set; }

        [JsonIgnore]
        public ForecastAstrology? Astronomy => AstronomyValue?.Data;
        
        [JsonPropertyName("error")]
        public WeatherError? Error { get; set; }
    }
    public class WeatherError
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }
        [JsonPropertyName("message")]
        public string Message { get; set; } = "";
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
        public string Text { get; set; } = string.Empty;
        [JsonPropertyName("icon")]
        public string IconUrl { get; set; } = string.Empty;
        [JsonPropertyName("code")]
        public int Code { get; set; }
    }
    public class WeatherLocation
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        [JsonPropertyName("region")]
        public string Region { get; set; } = string.Empty;
        [JsonPropertyName("country")]
        public string Country { get; set; } = string.Empty;
        [JsonPropertyName("lat")]
        public double Latitude { get; set; }
        [JsonPropertyName("lon")]
        public double Longitude { get; set; }
        [JsonPropertyName("tz_id")]
        public string TimezoneId { get; set; }
        [JsonPropertyName("localtime_epoch")]
        public long LocalTimestampEpoch { get; set; }
        [JsonPropertyName("localtime")]
        public string LocalTimestamp { get; set; } = "0";
    }
}
