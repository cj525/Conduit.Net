using System;

namespace Pipes.Exceptions
{
    public class NotAttachedException : Exception
    {
        public NotAttachedException(string message) : base(message)
        {
        }
    }
}