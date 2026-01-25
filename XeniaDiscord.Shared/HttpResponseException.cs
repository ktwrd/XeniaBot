using System;
using System.IO;
using System.Net.Http;

namespace XeniaBot.Shared;

/// <remarks>
/// Generated with
/// <see href="https://ktwrd.github.io/csharp-exception-generator.html"/>
/// </remarks>
public class HttpResponseException : Exception
{
    #region Constructors
    /// <inheritdoc/>
    public HttpResponseException() : base()
    { }

    /// <inheritdoc/>
    public HttpResponseException(string? message) : base(message)
    { }

    /// <inheritdoc/>
    public HttpResponseException(string? message, Exception? innerException) : base(message, innerException)
    { }
    #endregion

    /// <inheritdoc/>
    public override string ToString()
    {
        return base.ToString();
    }

    public HttpRequestMessage? Request { get; set; }
    public HttpResponseMessage? Response { get; set; }
    public MemoryStream? ResponseStream { get; set; }
}
