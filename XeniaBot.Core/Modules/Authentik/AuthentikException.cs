using System;

namespace XeniaBot.Core.Modules;

public class AuthentikException : Exception
{
    public string? Detail { get; private set; }
    public string? ErrorCode { get; private set; }
    
    public AuthentikException() : base()
    {}
    public AuthentikException(string? message) : base(message)
    {}
    public AuthentikException(string? message, Exception innerException) : base(message, innerException)
    {}

    public override string ToString()
    {
        if (Detail == null)
            return base.ToString();
        
        if (ErrorCode != null)
            return $"{Detail} (ErrorCode: {ErrorCode})\n{StackTrace}";

        return $"{Detail}\n{StackTrace}";
    }

    public AuthentikException(AuthentikGenericAPIError apiError)
        : base()
    {
        Detail = apiError.Detail;
        ErrorCode = apiError.ErrorCode;
    }
}