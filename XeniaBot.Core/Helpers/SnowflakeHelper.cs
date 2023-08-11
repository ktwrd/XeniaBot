using IdGen;
using System;
using XeniaBot.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XeniaBot.Core.Helpers
{
    public static class SnowflakeHelper
    {
        public static readonly DateTime SnowflakeEpoch = new DateTime(2010, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static IdGenerator Create(int generatorId)
        {
            var structure = new IdStructure(43, 2, 18);
            var options = new IdGeneratorOptions(structure, new DefaultTimeSource(SnowflakeEpoch));
            var generator = new IdGenerator(generatorId, options);

#if DEBUG
            Log.Debug($"Maximum Generators:    {structure.MaxGenerators}");
            Log.Debug($"Id's/ms per generator: {structure.MaxSequenceIds}");
            Log.Debug($"Id's/ms total:         {structure.MaxGenerators * structure.MaxSequenceIds}");
#endif

            return generator;
        }
    }
}
