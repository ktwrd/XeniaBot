using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkidBot.Core
{
    public class NonfatalException : Exception
    {
        public NonfatalException() : base ()
        {
        }

        public NonfatalException(string? message) : base (message)
        {
        }

        public NonfatalException(string? message, Exception innerException) : base (message, innerException)
        {
        }
    }
}
