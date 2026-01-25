using System.Text.Json.Serialization;

namespace XeniaBot.Shared.Schema.WeatherAPI
{
    public class AirQuality
    {
        // Carbon Monoxide (μg/m3)
        [JsonPropertyName("co")]
        public double CarbonMonoxide;

        /// <summary>
        /// Ozone (μg/m3)
        /// </summary>
        [JsonPropertyName("o3")]
        public double Ozone;

        /// <summary>
        /// Nitrogen Dioxide (μg/m3)
        /// </summary>
        [JsonPropertyName("no2")]
        public double Nitrogen;

        /// <summary>
        /// Sulphur dioxide (μg/m3)
        /// </summary>
        [JsonPropertyName("so2")]
        public double Sulphur;

        /// <summary>
        /// PM2.5 (μg/m3)
        /// </summary>
        [JsonPropertyName("pm2_5")]
        public double PM25;

        /// <summary>
        /// PM10 (μg/m3)
        /// </summary>
        [JsonPropertyName("pm10")]
        public double PM10;

        /// <summary>
        /// United States EPA Standard Index
        /// </summary>
        [JsonPropertyName("us-epa-index")]
        public int EPAValue = 0;
        /// <summary>
        /// United States EPA Standard cast to user-friendly names
        /// </summary>
        public USEPAIndex EPA
        {
            get
            {
                if (EPAValue < 1 || EPAValue > 6)
                    return USEPAIndex.Unknown;
                return (USEPAIndex)EPAValue;
            }
        }

        /// <summary>
        /// UK Defra Index Value
        /// </summary>
        [JsonPropertyName("gb-defra-index")]
        public int DefraValue = 0;
        /// <summary>
        /// UK Defra Index casted to band
        /// </summary>
        public UKDefraBand DefraBand
        {
            get
            {
                if (DefraValue < 1)
                    return UKDefraBand.Unknown;
                else if (DefraValue >= 1 && DefraValue <= 3)
                    return UKDefraBand.Low;
                else if (DefraValue >= 4 && DefraValue <= 6)
                    return UKDefraBand.Moderate;
                else if (DefraValue >= 7 && DefraValue <= 9)
                    return UKDefraBand.High;
                else
                    return UKDefraBand.VeryHigh;
            }
        }
    }
}
