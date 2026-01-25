using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace XeniaBot.Shared;

/// <summary>
/// Exception used when there are issues reading or finding an embedded resource.
/// </summary>
/// <remarks>
/// Generated with
/// <see href="https://ktwrd.github.io/csharp-exception-generator.html"/>
/// </remarks>
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
    public Assembly? Assembly { get; set; }
    /// <summary>
    /// Enumerable of Assemblies that the resource was searched in.
    /// </summary>
    public IList<Assembly>? SearchedAssemblies { get; set; }
    /// <summary>
    /// Full name of the resource (asm.namespace.file)
    /// </summary>
    public string? ResourceName { get; set; }
    /// <summary>
    /// Does the resource exist? Will be <see langword="null"/> if this doesn't matter.
    /// </summary>
    public bool? ResourceExists { get; set; }

    /// <inheritdoc/>
    public override string ToString()
    {
        if (Assembly != null || ResourceName != null || ResourceExists != null)
        {
            var sb = new StringBuilder();
            sb.Append(base.ToString());

            sb.AppendLine();
            sb.Append("".PadRight(40, '-'));
            sb.AppendFormat(" {0}", GetType().Name);
            sb.AppendLine();
            sb.AppendLine(string.Format("Assembly: {0}", Assembly?.ToString()));
            if (SearchedAssemblies?.Count > 0)
            {
                for (int i = 0; i < SearchedAssemblies.Count; i++)
                {
                    var s = string.Format("SearchedAssemblies[{0}]: {1}",
                        i,
                        SearchedAssemblies[i].ToString());
                    sb.AppendLine(s);
                }
            }
            sb.AppendLine(string.Format("ResourceName: {0}", ResourceName?.ToString()));
            sb.AppendLine(string.Format("ResourceExists: {0}", ResourceExists?.ToString()));
            return sb.ToString();
        }
        return base.ToString();
    }
}
