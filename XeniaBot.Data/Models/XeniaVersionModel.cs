using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using XeniaBot.Shared.Models;

namespace XeniaBot.Data.Models
{
    public class XeniaVersionModel : BaseModelGuid
    {
        public static string CollectionName => "xenia_versions";

        /// <summary>
        /// <inheritdoc cref="XeniaVersionAssemblyItem.Name"/>
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// <inheritdoc cref="XeniaVersionAssemblyItem.Version"/>
        /// </summary>
        public string Version { get; set; }
        /// <summary>
        /// <para>Build Timestamp parsed from the Version.</para>
        ///
        /// <para>Unix Timestamp (Seconds, UTC)</para>
        /// </summary>
        public long ParsedVersionTimestamp { get; set; }

        /// <summary>
        /// DateTimeOffset parsed from <see cref="ParsedVersionTimestamp"/>
        /// </summary>
        [BsonIgnore]
        public DateTimeOffset ParsedVersionTime => DateTimeOffset.FromUnixTimeSeconds(ParsedVersionTimestamp);
        /// <summary>
        /// Unix Timestamp (UTC, Seconds)
        /// </summary>
        public long CreatedAt { get; set; }
        /// <summary>
        /// List of all loaded assemblies
        /// </summary>
        public List<XeniaVersionAssemblyItem> Assemblies { get; set; }
        public Dictionary<string, object> Flags { get; set; }

        /// <summary>
        /// Get an item in <see cref="Assemblies"/> where <see cref="XeniaVersionAssemblyItem.Name"/> is equal to the <paramref name="name"/> provided.
        /// </summary>
        public XeniaVersionAssemblyItem? GetAssemblyByName(string name)
        {
            return Assemblies.FirstOrDefault(v => v.Name == name);
        }
        
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

        /// <summary>
        /// <inheritdoc cref="FillAssemblies(Assembly[])"/>
        ///
        /// <para>Uses <see cref="AppDomain.GetAssemblies()"/> from <see cref="AppDomain.CurrentDomain"/></para>
        /// </summary>
        public void FillAssemblies()
        {
            FillAssemblies(AppDomain.CurrentDomain.GetAssemblies().ToArray());
        }
        
        /// <summary>
        /// Fill the contents of <see cref="Assemblies"/> based off <paramref name="assemblyArray"/>
        /// </summary>
        /// <param name="assemblyArray">Assemblies to load</param>
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
