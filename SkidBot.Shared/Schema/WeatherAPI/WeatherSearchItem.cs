using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SkidBot.Shared.Schema.WeatherAPI
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
