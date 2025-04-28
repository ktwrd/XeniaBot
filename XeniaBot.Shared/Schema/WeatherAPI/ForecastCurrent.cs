using System;
using System.Text.Json.Serialization;

namespace XeniaBot.Shared.Schema.WeatherAPI
{
    public class ForecastCurrent : BaseForecastData
    {
        /// <summary>
        /// Local time when the real time data was updated.
        /// </summary>
        [JsonPropertyName("last_updated")]
        public string LastUpdatedString = "1970-01-01 00:00";
        /// <summary>
        /// Local time when the real time data was updated in unix time seconds.
        /// </summary>
        [JsonPropertyName("last_updated_epoch")]
        public int LastUpdatedTimestamp = 0;
        /// <summary>
        /// <see cref="LastUpdatedTimestamp"/> parsed with <see cref="DateTimeOffset.FromUnixTimeSeconds(long)"/>
        /// </summary>
        public DateTime LastUpdated
        {
            get
            {
                return DateTimeOffset.FromUnixTimeSeconds(LastUpdatedTimestamp).DateTime;
            }
        }
        /// <summary>
        /// Null when fetched with "aqi" url parameter to "no"
        /// </summary>
        [JsonPropertyName("air_quality")]
        public AirQuality? AirQuality = null;
    }
}
