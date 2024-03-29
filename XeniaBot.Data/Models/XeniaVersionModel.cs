using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XeniaBot.Data.Models
{
    public class XeniaVersionModel
    {
        public static string CollectionName => "xenia_versions";
        
        [BsonElement("_id")]
        public string Id { get; set; }

        public string Name { get; set; }
        public string Version { get; set; }
        public long ParsedVersionTimestamp { get; set; }
        /// <summary>
        /// Unix Timestamp (UTC, Seconds)
        /// </summary>
        public long CreatedAt { get; set; }

        public Dictionary<string, object> Flags { get; set; }

        public XeniaVersionModel()
        {
            Id = Guid.NewGuid().ToString();
            Name = "";
            Version = "";
            ParsedVersionTimestamp = 0;
            CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            Flags = new Dictionary<string, object>();
        }
    }
}
