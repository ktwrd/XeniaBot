using System.Text.Json.Serialization;

namespace XeniaBot.Shared.Schema.WeatherAPI
{
    public class AirQuality
    {
        // Carbon Monoxide (μg/m3)
        [JsonPropertyName("co")]
        public double CarbonMonoxide { get; set; }

        /// <summary>
        /// Ozone (μg/m3)
        /// </summary>
        [JsonPropertyName("o3")]
        public double Ozone { get; set; }

        /// <summary>
        /// Nitrogen Dioxide (μg/m3)
        /// </summary>
        [JsonPropertyName("no2")]
        public double Nitrogen { get; set; }

        /// <summary>
        /// Sulphur dioxide (μg/m3)
        /// </summary>
        [JsonPropertyName("so2")]
        public double Sulphur { get; set; }

        /// <summary>
        /// PM2.5 (μg/m3)
        /// </summary>
        [JsonPropertyName("pm2_5")]
        public double PM25 { get; set; }

        /// <summary>
        /// PM10 (μg/m3)
        /// </summary>
        [JsonPropertyName("pm10")]
        public double PM10 { get; set; }

        /// <summary>
        /// United States EPA Standard Index
        /// </summary>
        [JsonPropertyName("us-epa-index")]
        public int EPAValue { get; set; } = 0;
        /// <summary>
        /// United States EPA Standard cast to user-friendly names
        /// </summary>
        public USEPAIndex EPA
        {
            get
            {
                if (EPAValue is < 1 or > 6)
                    return USEPAIndex.Unknown;
                return (USEPAIndex)EPAValue;
            }
        }

        /// <summary>
        /// UK Defra Index Value
        /// </summary>
        [JsonPropertyName("gb-defra-index")]
        public int DefraValue { get; set; } = 0;
        /// <summary>
        /// UK Defra Index cast to band
        /// </summary>
        public UKDefraBand DefraBand
        {
            get
            {
                return DefraValue switch
                {
                    < 1 => UKDefraBand.Unknown,
                    <= 3 => UKDefraBand.Low,
                    <= 6 => UKDefraBand.Moderate,
                    <= 9 => UKDefraBand.High,
                    _ => UKDefraBand.VeryHigh
                };
            }
        }
    }
}
