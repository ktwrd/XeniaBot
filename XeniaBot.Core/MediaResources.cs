using System;
using System.IO;
using System.Linq;
using System.Reflection;
using XeniaBot.Shared;

namespace XeniaBot.Core;

public static class MediaResources
{
    public static Stream GetStream(string name, params Assembly[] assemblies)
    {
        foreach (var asm in assemblies)
        {
            var names = asm.GetManifestResourceNames();
            var resourceName = names.FirstOrDefault(e => e == name);
            if (resourceName == null)
            {
                continue;
            }
            var stream = asm.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                throw new EmbeddedResourceException("Resource stream is null, but it exists in the assembly!")
                {
                    Assembly = asm,
                    SearchedAssemblies = assemblies,
                    ResourceName = name,
                    ResourceExists = true,
                };
            }
            return stream;
        }
        throw new EmbeddedResourceException("Could not find resource in any of the assemblies provided.")
        {
            Assembly = null,
            SearchedAssemblies = assemblies,
            ResourceName = name,
            ResourceExists = false
        };
    }
    public static Stream GetStream(string name)
    {
        return GetStream(name, AppDomain.CurrentDomain.GetAssemblies());
    }

    public static Stream ImageSpeech => GetStream("XeniaBot.Core.Resources.speech.png");
    public static Stream ImageSpeechBubble => GetStream("XeniaBot.Core.Resources.speechbubble.png");
}
