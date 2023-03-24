using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SkidBot.Shared.Schema.OpenTDB
{
    public class OpenTDBResponse
    {
        [JsonPropertyName("response_code")]
        public int ResponseCodeValue = -1;
        public OpenTDBResponseCode ResponseCode => (OpenTDBResponseCode)ResponseCodeValue;
        [JsonPropertyName("results")]
        public OpenTDBQuestion[] Results = Array.Empty<OpenTDBQuestion>();
    }
    public enum OpenTDBResponseCode
    {
        Unknown = -1,
        Success = 0,
        NoResults = 1,
        InvalidParameter = 2,
        TokenNotFound = 3,
        TokenEmpty = 4
    }
}
