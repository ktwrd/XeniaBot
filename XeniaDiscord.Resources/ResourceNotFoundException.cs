using System.Reflection;
using System.Text;

namespace XeniaDiscord.Resources;

/// <summary>
/// Exception used when there are issues reading or finding an embedded resource.
/// </summary>
public class EmbeddedResourceException : Exception
{
    #region Constructors
    /// <inheritdoc/>
    public EmbeddedResourceException() : base()
    { }

    /// <inheritdoc/>
    public EmbeddedResourceException(string? message) : base(message)
    { }

    /// <inheritdoc/>
    public EmbeddedResourceException(string? message, Exception? innerException) : base(message, innerException)
    { }
    #endregion

    /// <summary>
    /// Assembly that was being used when reading (or searching) for <see cref="ResourceName"/>
    /// </summary>
    public Assembly? Assembly { get; init; }
    /// <summary>
    /// Enumerable of Assemblies that the resource was searched in.
    /// </summary>
    public IReadOnlyCollection<Assembly>? SearchedAssemblies { get; init; }
    /// <summary>
    /// Full name of the resource (asm.namespace.file)
    /// </summary>
    public string? ResourceName { get; init; }
    /// <summary>
    /// Does the resource exist? Will be <see langword="null"/> if this doesn't matter.
    /// </summary>
    public bool? ResourceExists { get; init; }

    /// <inheritdoc/>
    public override string ToString()
    {
        if (Assembly == null && ResourceName == null && ResourceExists == null) return base.ToString();
        
        var sb = new StringBuilder();
        sb.Append(base.ToString());

        sb.AppendLine();
        sb.Append("".PadRight(40, '-'));
        sb.Append($" {GetType().Name}");
        sb.AppendLine();
        sb.AppendLine($"{nameof(Assembly)}: {Assembly}");
        foreach (var (index, item) in SearchedAssemblies?.Index() ?? [])
        {
            sb.AppendLine($"{nameof(SearchedAssemblies)}[{index}]: {item}");
        }
        sb.AppendLine($"{nameof(ResourceName)}: {ResourceName}");
        sb.AppendLine($"{nameof(ResourceExists)}: {ResourceExists}");
        return sb.ToString();
    }
}