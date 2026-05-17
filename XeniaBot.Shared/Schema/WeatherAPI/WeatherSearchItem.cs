using System;
using System.Text.Json.Serialization;

namespace XeniaBot.Shared.Schema.WeatherAPI
{
    public class WeatherSearchItem
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
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
        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }
    public class WeatherSearchResponse
    {
        public WeatherSearchItem[] Response { get; set; } = [];
        public WeatherError? Error { get; set; }
    }
}
