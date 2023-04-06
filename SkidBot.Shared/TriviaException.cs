using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkidBot.Shared
{
    public class TriviaException : Exception
    {
        public TriviaException()
            : base()
        { }

        public TriviaException(string? message)
            : base(message)
        { }
        public TriviaException(string? message, Exception? innerException)
            : base(message, innerException)
        { }
    }
}
