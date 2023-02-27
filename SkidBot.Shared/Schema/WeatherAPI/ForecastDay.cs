using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SkidBot.Shared.Schema.WeatherAPI
{
    public class ForecastDay
    {
        [JsonPropertyName("date")]
        public string DateString;
        [JsonPropertyName("date_epoch")]
        public long DateTimestamp;
        public DateTime Date
        {
            get
            {
                return DateTimeOffset.FromUnixTimeSeconds(DateTimestamp).Date;
            }
        }
        [JsonPropertyName("day")]
        public ForecastDayChild? Day;
        [JsonPropertyName("astro")]
        public ForecastAstrology? Astrology;
        [JsonPropertyName("hour")]
        public ForecastHourItem[] Hour;

        public ForecastDay()
        {
            DateString = "1970-01-01 00:00";
            DateTimestamp = 0;
            Day = null;
            Astrology = null;
            Hour = Array.Empty<ForecastHourItem>();
        }
    }
    public class ForecastDayChild
    {
        [JsonPropertyName("maxtemp_c")]
        public double TemperatureMaximumCelcius;
        [JsonPropertyName("maxtemp_f")]
        public double TemperatureMaximumFahrenheit;
        [JsonPropertyName("mintemp_c")]
        public double TemperatureMinimumCelcius;
        [JsonPropertyName("mintemp_f")]
        public double TemperatureMinimumFahrenheit;
        [JsonPropertyName("avgtemp_c")]
        public double TemperatureAverageCelcius;
        [JsonPropertyName("avgtemp_f")]
        public double TemperatureAverageFahrenheit;
        [JsonPropertyName("maxwind_mph")]
        public double WindSpeedMaximumMph;
        [JsonPropertyName("maxwind_kph")]
        public double WindSpeedMaximumKph;
        [JsonPropertyName("totalprecip_mm")]
        public double TotalPrecipitationMm;
        [JsonPropertyName("totalprecip_in")]
        public double TotalPrecipitationIn;
        [JsonPropertyName("totalsnow_cm")]
        public double TotalSnow;
        [JsonPropertyName("avgvis_km")]
        public double VisibilityAverageKm;
        [JsonPropertyName("avgvis_miles")]
        public double VisibilityAverageMiles;
        [JsonPropertyName("avghumidity")]
        public double HumidityAverage;
        [JsonPropertyName("daily_will_it_rain")]
        public int WillItRainValue = 0;
        public bool WillItRain => WillItRainValue == 1;
        [JsonPropertyName("daily_chance_of_rain")]
        public int ChanceOfRain;
        [JsonPropertyName("daily_will_it_snow")]
        public int WillItSnowValue = 0;
        public bool WillItSnow => WillItSnowValue == 1;
        [JsonPropertyName("daily_chance_of_snow")]
        public int ChanceOfSnow;
        [JsonPropertyName("condition")]
        public WeatherCondition? Condition = null;
        [JsonPropertyName("uv")]
        public double UV;
    }
    public class ForecastAstrology
    {
        [JsonPropertyName("sunrise")]
        public string Sunrise = "00:00 AM";
        [JsonPropertyName("sunset")]
        public string Sunset = "00:00 AM";
        [JsonPropertyName("moonrise")]
        public string Moonrise = "00:00 AM";
        [JsonPropertyName("moonset")]
        public string Moonset = "00:00 AM";
        [JsonPropertyName("moon_phase")]
        public string MoonPhase = "00:00 AM";
        [JsonPropertyName("moon_illumination")]
        public string MoonIllumination = "00:00 AM";
        [JsonPropertyName("is_moon_up")]
        public int IsMoonUpValue = 0;
        [JsonPropertyName("is_sun_up")]
        public int IsSunUpValue = 0;
        public bool IsMoonUp => IsMoonUpValue == 1;
        public bool IsSunUp => IsSunUpValue == 1;
    }
    public class AstronomyParent
    {
        [JsonPropertyName("astro")]
        public ForecastAstrology? Data = null;
    }
    public class ForecastHourItem : BaseForecastData
    {
        /// <summary>
        /// Time as epoch in seconds
        /// </summary>
        [JsonPropertyName("time_epoch")]
        public long Timestamp = 0;
        /// <summary>
        /// Date and time as YYYY-MM-DD hh:mm
        /// </summary>
        [JsonPropertyName("time")]
        public string TimeValue = "1970-01-01 00:00";
        /// <summary>
        /// Date parsed from <see cref="Timestamp"/>
        /// </summary>
        public DateTime Time
        {
            get
            {
                return DateTimeOffset.FromUnixTimeSeconds(Timestamp).DateTime;
            }
        }


        /// <summary>
        /// Chance of rain as percentage
        /// </summary>
        [JsonPropertyName("chance_of_rain")]
        public int ChanceOfRain;
        /// <summary>
        /// Chance of snow as percentage
        /// </summary>
        [JsonPropertyName("chance_of_snow")]
        public int ChanceOfSnow;
        /// <summary>
        /// Dew point in celcius
        /// </summary>
        [JsonPropertyName("dewpoint_c")]
        public double DewPointCelcius;
        /// <summary>
        /// Dew point in fahrenheit
        /// </summary>
        [JsonPropertyName("dewpoint_f")]
        public double DewPointFarenheit;

        /// <summary>
        /// Heat index in celcius
        /// </summary>
        [JsonPropertyName("heatindex_c")]
        public double HeatIndexCelcius;
        /// <summary>
        /// Heat index in fahrenheit
        /// </summary>
        [JsonPropertyName("heatindex_f")]
        public double HeatIndexFarenheit;

        /// <summary>
        /// 1 = Yes 0 = No
        /// </summary>
        [JsonPropertyName("will_it_rain")]
        public int WillItRainValue = 0;
        /// <summary>
        /// 1 = Yes 0 = No
        /// </summary>
        [JsonPropertyName("will_it_snow")]
        public int WillItSnowValue = 0;
        /// <summary>
        /// Will it will rain or not
        /// </summary>
        public bool WillItRain => WillItRainValue == 1;
        /// <summary>
        /// Will it snow or not
        /// </summary>
        public bool WillItSnow => WillItSnowValue == 1;
    }
}
