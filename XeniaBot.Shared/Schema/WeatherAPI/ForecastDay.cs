using System;
using System.Text.Json.Serialization;

namespace XeniaBot.Shared.Schema.WeatherAPI
{
    public class ForecastDay
    {
        [JsonPropertyName("date")]
        public string DateString { get; set; }
        [JsonPropertyName("date_epoch")]
        public long DateTimestamp { get; set; }
        [JsonIgnore]
        public DateTime Date
            => DateTimeOffset.FromUnixTimeSeconds(DateTimestamp).Date;

        [JsonPropertyName("day")]
        public ForecastDayChild? Day { get; set; }
        [JsonPropertyName("astro")]
        public ForecastAstrology? Astrology { get; set; }
        [JsonPropertyName("hour")]
        public ForecastHourItem[] Hour { get; set; }

        public ForecastDay()
        {
            DateString = "1970-01-01 00:00";
            DateTimestamp = 0;
            Day = null;
            Astrology = null;
            Hour = [];
        }
    }
    public class ForecastDayChild
    {
        [JsonPropertyName("maxtemp_c")]
        public double TemperatureMaximumCelcius { get; set; }
        [JsonPropertyName("maxtemp_f")]
        public double TemperatureMaximumFahrenheit { get; set; }
        [JsonPropertyName("mintemp_c")]
        public double TemperatureMinimumCelcius { get; set; }
        [JsonPropertyName("mintemp_f")]
        public double TemperatureMinimumFahrenheit { get; set; }
        [JsonPropertyName("avgtemp_c")]
        public double TemperatureAverageCelcius { get; set; }
        [JsonPropertyName("avgtemp_f")]
        public double TemperatureAverageFahrenheit { get; set; }
        [JsonPropertyName("maxwind_mph")]
        public double WindSpeedMaximumMph { get; set; }
        [JsonPropertyName("maxwind_kph")]
        public double WindSpeedMaximumKph { get; set; }
        [JsonPropertyName("totalprecip_mm")]
        public double TotalPrecipitationMm { get; set; }
        [JsonPropertyName("totalprecip_in")]
        public double TotalPrecipitationIn { get; set; }
        [JsonPropertyName("totalsnow_cm")]
        public double TotalSnow { get; set; }
        [JsonPropertyName("avgvis_km")]
        public double VisibilityAverageKm { get; set; }
        [JsonPropertyName("avgvis_miles")]
        public double VisibilityAverageMiles { get; set; }
        [JsonPropertyName("avghumidity")]
        public double HumidityAverage { get; set; }
        [JsonPropertyName("daily_will_it_rain")]
        public int WillItRainValue { get; set; } = 0;
        public bool WillItRain => WillItRainValue == 1;
        [JsonPropertyName("daily_chance_of_rain")]
        public int ChanceOfRain { get; set; }
        [JsonPropertyName("daily_will_it_snow")]
        public int WillItSnowValue { get; set; } = 0;
        public bool WillItSnow => WillItSnowValue == 1;
        [JsonPropertyName("daily_chance_of_snow")]
        public int ChanceOfSnow { get; set; }
        [JsonPropertyName("condition")]
        public WeatherCondition? Condition { get; set; }
        [JsonPropertyName("uv")]
        public double UV { get; set; }
    }
    public class ForecastAstrology
    {
        [JsonPropertyName("sunrise")]
        public string Sunrise { get; set; } = "00:00 AM";
        [JsonPropertyName("sunset")]
        public string Sunset { get; set; } = "00:00 AM";
        [JsonPropertyName("moonrise")]
        public string Moonrise { get; set; } = "00:00 AM";
        [JsonPropertyName("moonset")]
        public string Moonset { get; set; } = "00:00 AM";
        [JsonPropertyName("moon_phase")]
        public string MoonPhase { get; set; } = "00:00 AM";
        [JsonPropertyName("moon_illumination")]
        public int MoonIllumination { get; set; } = 0;
        [JsonPropertyName("is_moon_up")]
        public int IsMoonUpValue { get; set; } = 0;
        [JsonPropertyName("is_sun_up")]
        public int IsSunUpValue { get; set; } = 0;
        [JsonIgnore]
        public bool IsMoonUp => IsMoonUpValue == 1;
        [JsonIgnore]
        public bool IsSunUp => IsSunUpValue == 1;
    }
    public class AstronomyParent
    {
        [JsonPropertyName("astro")]
        public ForecastAstrology? Data { get; set; }
    }
    public class ForecastHourItem : BaseForecastData
    {
        /// <summary>
        /// Time as epoch in seconds
        /// </summary>
        [JsonPropertyName("time_epoch")]
        public long Timestamp { get; set; } = 0;
        /// <summary>
        /// Date and time as YYYY-MM-DD hh:mm
        /// </summary>
        [JsonPropertyName("time")]
        public string TimeValue { get; set; } = "1970-01-01 00:00";
        /// <summary>
        /// Date parsed from <see cref="Timestamp"/>
        /// </summary>
        [JsonIgnore]
        public DateTime Time
            => DateTimeOffset.FromUnixTimeSeconds(Timestamp).DateTime;


        /// <summary>
        /// Chance of rain as percentage
        /// </summary>
        [JsonPropertyName("chance_of_rain")]
        public int ChanceOfRain { get; set; }
        /// <summary>
        /// Chance of snow as percentage
        /// </summary>
        [JsonPropertyName("chance_of_snow")]
        public int ChanceOfSnow { get; set; }
        /// <summary>
        /// Dew point in celcius
        /// </summary>
        [JsonPropertyName("dewpoint_c")]
        public double DewPointCelcius { get; set; }
        /// <summary>
        /// Dew point in fahrenheit
        /// </summary>
        [JsonPropertyName("dewpoint_f")]
        public double DewPointFarenheit { get; set; }

        /// <summary>
        /// Heat index in celcius
        /// </summary>
        [JsonPropertyName("heatindex_c")]
        public double HeatIndexCelcius { get; set; }
        /// <summary>
        /// Heat index in fahrenheit
        /// </summary>
        [JsonPropertyName("heatindex_f")]
        public double HeatIndexFarenheit { get; set; }

        /// <summary>
        /// 1 = Yes 0 = No
        /// </summary>
        [JsonPropertyName("will_it_rain")]
        public int WillItRainValue { get; set; } = 0;
        /// <summary>
        /// 1 = Yes 0 = No
        /// </summary>
        [JsonPropertyName("will_it_snow")]
        public int WillItSnowValue { get; set; } = 0;
        /// <summary>
        /// Will it will rain or not
        /// </summary>
        [JsonIgnore]
        public bool WillItRain => WillItRainValue == 1;
        /// <summary>
        /// Will it snow or not
        /// </summary>
        [JsonIgnore]
        public bool WillItSnow => WillItSnowValue == 1;
    }
}
