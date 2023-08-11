using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace XeniaBot.Shared.Schema.WeatherAPI
{
    public class WeatherAlertItem
    {
        [JsonPropertyName("headline")]
        public string Headline = "";
        [JsonPropertyName("msgType")]
        public string MessageType = "";
        [JsonPropertyName("severity")]
        public string Severity = "";
        [JsonPropertyName("urgency")]
        public string Urgency = "";
        [JsonPropertyName("areas")]
        public string Areas = "";
        [JsonPropertyName("category")]
        public string Category = "";
        [JsonPropertyName("certainty")]
        public string Certainty = "";
        [JsonPropertyName("event")]
        public string Event = "";
        [JsonPropertyName("note")]
        public string Note = "";
        /// <summary>
        /// Alert Description
        /// </summary>
        [JsonPropertyName("desc")]
        public string Description = "";
        /// <summary>
        /// Instructions
        /// </summary
        [JsonPropertyName("instruction")]
        public string Instructions = "";

        /// <summary>
        /// When weather alert is effective of.
        /// </summary>
        [JsonPropertyName("effective")]
        public string EffectiveDateValue = "1970-01-01T00:00:00+00:00";
        /// <summary>
        /// When weather alert expires
        /// </summary>
        [JsonPropertyName("expires")]
        public string ExpiresDateValue = "1970-01-01T00:00:00+00:00";

        /// <summary>
        /// <see cref="EffectiveDateValue"/> piped through <see cref="DateTime.Parse(string)"/>
        /// </summary>
        public DateTime EffectiveDate
        {
            get
            {
                return DateTime.Parse(EffectiveDateValue);
            }
        }
        /// <summary>
        /// <see cref="ExpiresDateValue"/> piped through <see cref="DateTime.Parse(string)"/>
        /// </summary>
        public DateTime ExpiresDate
        {
            get
            {
                return DateTime.Parse(ExpiresDateValue);
            }
        }
    }
}
