using System;
using Pipes.Exceptions.Abstraction;

namespace Pipes.Exceptions
{
    public class ProxyAlreadyAssignedException : TypeBasedMessageException
    {
        private ProxyAlreadyAssignedException(string message, Type type) : base(message, type)
        {
        }

        public static ProxyAlreadyAssignedException ForType<T>(string message)
        {
            return new ProxyAlreadyAssignedException(message, typeof (T));
        }
    }
}
