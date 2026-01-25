using System;
using System.Text.Json.Serialization;

namespace XeniaBot.Shared.Schema.WeatherAPI
{
    public class ForecastParent
    {
        [JsonPropertyName("forecastday")]
        public ForecastDay[] ForecastDay = Array.Empty<ForecastDay>();
    }
}
