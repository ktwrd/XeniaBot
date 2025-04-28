using System;
using System.IO;
using System.Reflection;
using XeniaBot.Shared;

namespace XeniaBot.Core;

public static class MediaResources
{
    public static Stream GetStream(string name, params Assembly[] assemblies)
    {
        foreach (var asm in assemblies)
        {
            foreach (var res in asm.GetManifestResourceNames())
            {
                if (res == name)
                {
                    var stream = asm.GetManifestResourceStream(res);
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
            }
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
