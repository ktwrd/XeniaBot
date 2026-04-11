using System.Reflection;
using System.Security.Cryptography;

namespace XeniaDiscord.Resources;

/// <summary>
/// Helper class for fetching Database Permission Verification Resources.
/// </summary>
public static class ResourceHelper
{
    private static Dictionary<string, string> TextCache => [];
    private static Dictionary<string, string> HashCache => [];

    /// <summary>
    /// Get all loaded assemblies (including this one)
    /// </summary>
    private static Assembly[] GetAssemblies()
    {
        return new[]
        {
            typeof(ResourceHelper).Assembly
        }.Concat(AppDomain.CurrentDomain.GetAssemblies()).ToArray();
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
            var targetName = asm.GetManifestResourceNames()
                .FirstOrDefault(e => e.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            if (string.IsNullOrEmpty(targetName))
                continue;

            var stream = asm.GetManifestResourceStream(targetName);
            if (stream == null)
            {
                throw new EmbeddedResourceException($"Stream for resource \"{targetName}\" is null, but it exists in the assembly!")
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

    /// <summary>
    /// Find Embedded Resource Stream with the provided <paramref name="name"/>.
    /// Will search in all loaded assemblies.
    /// </summary>
    /// <exception cref="EmbeddedResourceException">
    /// Thrown when <see cref="GetStream(string, Assembly[])"/> couldn't find
    /// the resource, or for some reason <see cref="Assembly.GetManifestResourceStream(string)"/>
    /// returns <see langword="null"/>
    /// </exception>
    internal static Stream GetStream(string name) => GetStream(name, GetAssemblies());

    /// <exception cref="EmbeddedResourceException">
    /// Thrown when <see cref="GetStream(string, Assembly[])"/> couldn't find
    /// the resource, or for some reason <see cref="Assembly.GetManifestResourceStream(string)"/>
    /// returns <see langword="null"/>
    /// </exception>
    public static string GetText(string name) => GetText(name, GetAssemblies());

    /// <exception cref="EmbeddedResourceException">
    /// Thrown when <see cref="GetStream(string, Assembly[])"/> couldn't find
    /// the resource, or for some reason <see cref="Assembly.GetManifestResourceStream(string)"/>
    /// returns <see langword="null"/>
    /// </exception>
    public static string GetText(string name, params Assembly[] assemblies)
    {
        lock (TextCache)
        {
            if (TextCache.TryGetValue(name, out var text)) return new(text);
            
            var stream = GetStream(name, assemblies);
            var reader = new StreamReader(stream);
            text = reader.ReadToEnd();
            TextCache.Add(name, text);

            return new(text);
        }
    }

    /// <exception cref="EmbeddedResourceException">
    /// Thrown when <see cref="GetStream(string, Assembly[])"/> couldn't find
    /// the resource, or for some reason <see cref="Assembly.GetManifestResourceStream(string)"/>
    /// returns <see langword="null"/>
    /// </exception>
    public static byte[] GetBytes(string name) => GetBytes(name, GetAssemblies());

    /// <exception cref="EmbeddedResourceException">
    /// Thrown when <see cref="GetStream(string, Assembly[])"/> couldn't find
    /// the resource, or for some reason <see cref="Assembly.GetManifestResourceStream(string)"/>
    /// returns <see langword="null"/>
    /// </exception>
    public static byte[] GetBytes(string name, params Assembly[] assemblies)
    {
        using var ms = GetMemoryStream(name, assemblies);
        return ms.ToArray();
    }

    /// <exception cref="EmbeddedResourceException">
    /// Thrown when <see cref="GetStream(string, Assembly[])"/> couldn't find
    /// the resource, or for some reason <see cref="Assembly.GetManifestResourceStream(string)"/>
    /// returns <see langword="null"/>
    /// </exception>
    public static MemoryStream GetMemoryStream(string name)
    {
        return GetMemoryStream(name, GetAssemblies());
    }

    /// <exception cref="EmbeddedResourceException">
    /// Thrown when <see cref="GetStream(string, Assembly[])"/> couldn't find
    /// the resource, or for some reason <see cref="Assembly.GetManifestResourceStream(string)"/>
    /// returns <see langword="null"/>
    /// </exception>
    public static MemoryStream GetMemoryStream(string name, params Assembly[] assemblies)
    {
        using var stream = GetStream(name, assemblies);
        var ms = new MemoryStream();
        stream.CopyTo(ms);
        ms.Seek(0, SeekOrigin.Begin);
        return ms;
    }

    /// <summary>
    /// <inheritdoc cref="GetHash(string, Assembly[]" path="/summary" />
    /// </summary>
    /// <exception cref="EmbeddedResourceException">
    /// Thrown when <see cref="GetStream(string, Assembly[])"/> couldn't find
    /// the resource, or for some reason <see cref="Assembly.GetManifestResourceStream(string)"/>
    /// returns <see langword="null"/>
    /// </exception>
    public static string GetHash(string name) => GetHash(name, GetAssemblies());
    
    /// <summary>
    /// Get the SHA256 hash of an embedded resource.
    /// If it has been computed before, then a cached result is returned.
    /// </summary>
    /// <exception cref="EmbeddedResourceException">
    /// Thrown when <see cref="GetStream(string, Assembly[])"/> couldn't find
    /// the resource, or for some reason <see cref="Assembly.GetManifestResourceStream(string)"/>
    /// returns <see langword="null"/>
    /// </exception>
    public static string GetHash(string name, params Assembly[] assemblies)
    {
        lock (HashCache)
        {
            if (HashCache.TryGetValue(name, out var hash)) return hash;

            using var stream = GetMemoryStream(name, assemblies);
            hash = Convert.ToHexStringLower(SHA256.HashData(stream));
            HashCache[name] = hash;
            return hash;
        }
    }

}