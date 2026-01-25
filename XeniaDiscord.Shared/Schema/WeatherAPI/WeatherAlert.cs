using System;
using System.Text.Json.Serialization;

namespace XeniaBot.Shared.Schema.WeatherAPI
{
    public class WeatherAlert
    {
        [JsonPropertyName("alert")]
        public WeatherAlertItem[] Items = Array.Empty<WeatherAlertItem>();
    }
}
