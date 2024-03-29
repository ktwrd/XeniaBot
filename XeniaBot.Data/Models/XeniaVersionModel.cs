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

        public List<XeniaVersionAssemblyItem> Assemblies { get; set; }
        public Dictionary<string, object> Flags { get; set; }

        public class XeniaVersionAssemblyItem
        {
            /// <summary>
            /// <para>Shortened Assembly Name</para>
            ///
            /// <para>e.g; XeniaBot.Data</para>
            /// </summary>
            public string Name { get; set; }
            /// <summary>
            /// <see cref="Assembly.FullName"/>
            /// </summary>
            public string FullName { get; set; }
            /// <summary>
            /// <see cref="Version.ToString()"/>
            /// </summary>
            public string Version { get; set; }

            public XeniaVersionAssemblyItem()
            {
                Name = "";
                FullName = "";
                Version = "0.0.0.0";
            }
        }
        public XeniaVersionModel()
        {
            Id = Guid.NewGuid().ToString();
            Name = "";
            Version = "";
            ParsedVersionTimestamp = 0;
            CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            Assemblies = new List<XeniaVersionAssemblyItem>();
            Flags = new Dictionary<string, object>();
        }
        public void FillAssemblies(Assembly[] assemblyArray)
        {
            var result = new XeniaVersionAssemblyItem[assemblyArray.Length];
            for (int i = 0; i < assemblyArray.Length; i++)
            {
                var asm = assemblyArray[i];
                var name = asm.FullName?.Split(",")[0];
                var version = asm.GetName().Version;
                var item = new XeniaVersionAssemblyItem()
                {
                    Name = name!,
                    FullName = asm.FullName!,
                    Version = version?.ToString() ?? "0.0.0.0"
                };
                result[i] = item;
            }

            Assemblies = result.ToList();
        }
    }
}
