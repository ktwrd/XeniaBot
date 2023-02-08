using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SkidBot.Core.Models
{
    public class TinyFoxImageModel
    {
        [JsonPropertyName("loc")]
        public string Location { get; set; }
        public string ImageLocation => $"https://api.tinyfox.dev{Location}";
        [JsonPropertyName("remaining_api_calls")]
        public string RemainingAPICalls { get; set; }
    }
}
