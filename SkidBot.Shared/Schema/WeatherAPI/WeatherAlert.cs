using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SkidBot.Shared.Schema.WeatherAPI
{
    public class WeatherAlert
    {
        [JsonPropertyName("alert")]
        public WeatherAlertItem[] Items = Array.Empty<WeatherAlertItem>();
    }
}
