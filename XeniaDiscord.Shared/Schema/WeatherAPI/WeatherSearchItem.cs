using System;
using System.Text.Json.Serialization;

namespace XeniaBot.Shared.Schema.WeatherAPI
{
    public class WeatherSearchItem
    {
        [JsonPropertyName("id")]
        public int Id;
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
        [JsonPropertyName("url")]
        public string Url;
    }
    public class WeatherSearchResponse
    {
        public WeatherSearchItem[] Response = Array.Empty<WeatherSearchItem>();
        public WeatherError? Error = null;
    }
}
