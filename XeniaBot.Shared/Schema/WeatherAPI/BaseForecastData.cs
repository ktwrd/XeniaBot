using System.Text.Json.Serialization;

namespace XeniaBot.Shared.Schema.WeatherAPI
{
    public class BaseForecastData
    {
        /// <summary>
        /// Temperature in celcius
        /// </summary>
        [JsonPropertyName("temp_c")]
        public double TemperatureCelcius { get; set; }
        /// <summary>
        /// Feels like temperature in celcius
        /// </summary>
        [JsonPropertyName("feelslike_c")]
        public double TemperatureFeelsLikeCelcius { get; set; }
        /// <summary>
        /// Temperature in fahrenheit
        /// </summary>
        [JsonPropertyName("temp_f")]
        public double TemperatureFahrenheit { get; set; }
        // Feels like temperature in fahrenheit
        [JsonPropertyName("feelslike_f")]
        public double TemperatureFeelsLikeFahrenheit { get; set; }


        [JsonPropertyName("is_day")]
        public int IsDayValue { get; set; } = 1;
        /// <summary>
        /// Whether to show day condition icon or night icon
        /// </summary>
        public bool IsDay => IsDayValue == 1;
        
        [JsonPropertyName("condition")]
        public WeatherCondition? Condition { get; set; }

        /// <summary>
        /// Wind speed in miles per hour
        /// </summary>
        [JsonPropertyName("wind_mph")]
        public double WindSpeedMph { get; set; }
        /// <summary>
        /// Wind speed in kilometres per hour
        /// </summary>
        [JsonPropertyName("wind_kph")]
        public double WindSpeedKph { get; set; }
        /// <summary>
        /// Wind direction in degrees
        /// </summary>
        [JsonPropertyName("wind_degree")]
        public double WindAngle { get; set; }
        /// <summary>
        /// Wind direction as a 16 point compass 
        /// </summary>
        [JsonPropertyName("wind_dir")]
        public string WindDirection { get; set; }

        /// <summary>
        /// Pressure in Millibars
        /// </summary>
        [JsonPropertyName("pressure_mb")]
        public double PressureMb { get; set; }
        /// <summary>
        /// Pressure in inches
        /// </summary>
        [JsonPropertyName("pressure_in")]
        public double PressureIn { get; set; }

        /// <summary>
        /// Precipitation in Millimetres
        /// </summary>
        [JsonPropertyName("precip_mm")]
        public double PrecipitationMm { get; set; }
        /// <summary>
        /// Precipitation in Inches
        /// </summary>
        [JsonPropertyName("precip_in")]
        public double PrecipitationIn { get; set; }

        /// <summary>
        /// Humidity as percentage (0 to 100)
        /// </summary>
        [JsonPropertyName("humidity")]
        public double Humidity { get; set; }
        /// <summary>
        /// Cloud Coverage as percentage (0 to 100)
        /// </summary>
        [JsonPropertyName("cloud")]
        public double CloudCoverage { get; set; }

        /// <summary>
        /// Visibility in kilometres
        /// </summary>
        [JsonPropertyName("vis_km")]
        public double VisibilityKm { get; set; }
        /// <summary>
        /// Visibility in miles
        /// </summary>
        [JsonPropertyName("vis_miles")]
        public double VisiblityMiles { get; set; }

        /// <summary>
        /// UV Levels
        /// </summary>
        [JsonPropertyName("uv")]
        public double UV { get; set; }

        /// <summary>
        /// Gust speed in miles per hour
        /// </summary>
        [JsonPropertyName("gust_mph")]
        public double GustMph { get; set; }
        /// <summary>
        /// Gust speed in kilometres per hour
        /// </summary>
        [JsonPropertyName("gust_kph")]
        public double GustKph { get; set; }
    }
}
