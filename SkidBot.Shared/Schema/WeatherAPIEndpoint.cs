using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SkidBot.Shared.Schema
{
    public static class WeatherAPIEndpoint
    {
        public static int[] StatusCodeError => new int[]
        {
            400,
            401,
            403
        };
        public static int[] StatusCodeSuccess => new int[]
        {
            200
        };
        public static string BaseUrl => "http://api.weatherapi.com";
        private static string encode(string value) => WebUtility.UrlEncode(value);
        public static string Current(string key, string location, bool airQuality)
        => $"{BaseUrl}/v1/current.json?key={encode(key)}&q={encode(location)}&aqi=" + (airQuality ? "yes" : "no");
        public static string Search(string key, string query)
        => $"{BaseUrl}/v1/search.json?key={encode(key)}&q={encode(query)}";
    }
}
