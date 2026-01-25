using System;

namespace XeniaBot.Shared;

public class TicketException : Exception
{
    public TicketException() : base()
    { }
    public TicketException(string? message) : base(message)
    { }
    public TicketException(string? message, Exception? innerException) : base(message, innerException)
    { }
}
