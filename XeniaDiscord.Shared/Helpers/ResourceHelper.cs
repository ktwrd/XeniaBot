using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace XeniaBot.Shared.Helpers;

public static class ResourceHelper
{
    /// <summary>
    /// Get all loaded assemblies (including this one)
    /// </summary>
    private static Assembly[] GetAssemblies()
    {
        var result = new Assembly[]
        {
            typeof(ResourceHelper).Assembly
        }.Concat(AppDomain.CurrentDomain.GetAssemblies()).ToArray();
        return result;
    }

    /// <summary>
    /// Find Embedded Resource Stream with the provided <paramref name="name"/>
    /// in all assemblies provided.
    /// </summary>
    /// <param name="name">Name of the embedded resource</param>
    /// <param name="assemblies">Assemblies to search in</param>
    /// <returns>
    /// Stream of the Embedded Resource.</returns>
    /// <exception cref="EmbeddedResourceException">
    /// Thrown when the resource couldn't be found, or
    /// <see cref="Assembly.GetManifestResourceStream(string)"/> returns
    /// <see langword="null"/>
    /// </exception>
    public static Stream GetStream(string name, params Assembly[] assemblies)
    {
        foreach (var asm in assemblies)
        {
            foreach (var res in asm.GetManifestResourceNames())
            {
                if (res.Equals(name, StringComparison.InvariantCultureIgnoreCase))
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
        throw new EmbeddedResourceException($"Could not find resource \"{name}\" in any of the assemblies provided.")
        {
            Assembly = null,
            SearchedAssemblies = assemblies,
            ResourceName = name,
            ResourceExists = false
        };
    }

    /// <summary>
    /// Find Embedded Resource Stream with the provided <paramref name="name"/>.
    /// Will search in all loaded assemblies.
    /// </summary>
    /// <exception cref="EmbeddedResourceException">
    /// Thrown when <see cref="GetStream(string, Assembly[])"/> couldn't find
    /// the resource, or for some reason <see cref="Assembly.GetManifestResourceStream(string)"/>
    /// returns <see langword="null"/>
    /// </exception>
    public static Stream GetStream(string name)
    {
        return GetStream(name, GetAssemblies());
    }
}
