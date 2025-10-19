using System;
using XeniaBot.Data.Models;

namespace XeniaBot.Data;

public class InnerTicketException : Exception
{
    #region Constructors
    public InnerTicketException(TicketModel ticketModel)
        : base()
    {
        Ticket = ticketModel;
    }

    public InnerTicketException(string? message, TicketModel ticketModel)
        : base(message)
    {
        Ticket = ticketModel;
    }

    public InnerTicketException(string? message, TicketModel ticketModel, Exception? innerException)
        : base(message, innerException)
    {
        Ticket = ticketModel;
    }
    #endregion
    
    public TicketModel Ticket { get; private set; }

    public override string ToString()
    {
        return base.ToString();
    }
}