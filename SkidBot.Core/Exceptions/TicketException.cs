using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkidBot.Core.Exceptions
{
    public class TicketException : Exception
    {
        public TicketException() : base()
        { }
        public TicketException(string? message) : base(message)
        { }
        public TicketException(string? message, Exception? innerException) : base(message, innerException)
        { }
    }
}
