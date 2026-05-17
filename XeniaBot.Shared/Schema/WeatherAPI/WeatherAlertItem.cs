using System;
using System.Text.Json.Serialization;

namespace XeniaBot.Shared.Schema.WeatherAPI
{
    public class WeatherAlertItem
    {
        [JsonPropertyName("headline")]
        public string Headline { get; set; } = "";
        [JsonPropertyName("msgType")]
        public string MessageType { get; set; } = "";
        [JsonPropertyName("severity")]
        public string Severity { get; set; } = "";
        [JsonPropertyName("urgency")]
        public string Urgency { get; set; } = "";
        [JsonPropertyName("areas")]
        public string Areas { get; set; } = "";
        [JsonPropertyName("category")]
        public string Category { get; set; } = "";
        [JsonPropertyName("certainty")]
        public string Certainty { get; set; } = "";
        [JsonPropertyName("event")]
        public string Event { get; set; } = "";
        [JsonPropertyName("note")]
        public string Note { get; set; } = "";
        /// <summary>
        /// Alert Description
        /// </summary>
        [JsonPropertyName("desc")]
        public string Description { get; set; } = "";
        /// <summary>
        /// Instructions
        /// </summary
        [JsonPropertyName("instruction")]
        public string Instructions { get; set; } = "";

        /// <summary>
        /// When weather alert is effective of.
        /// </summary>
        [JsonPropertyName("effective")]
        public string EffectiveDateValue { get; set; } = "1970-01-01T00:00:00+00:00";
        /// <summary>
        /// When weather alert expires
        /// </summary>
        [JsonPropertyName("expires")]
        public string ExpiresDateValue { get; set; } = "1970-01-01T00:00:00+00:00";

        /// <summary>
        /// <see cref="EffectiveDateValue"/> piped through <see cref="DateTime.Parse(string)"/>
        /// </summary>
        [JsonIgnore]
        public DateTime EffectiveDate => DateTime.Parse(EffectiveDateValue);

        /// <summary>
        /// <see cref="ExpiresDateValue"/> piped through <see cref="DateTime.Parse(string)"/>
        /// </summary>
        [JsonIgnore]
        public DateTime ExpiresDate => DateTime.Parse(ExpiresDateValue);
    }
}
