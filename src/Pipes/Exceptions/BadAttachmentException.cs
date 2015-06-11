using System;
using Pipes.Exceptions.Abstraction;

namespace Pipes.Exceptions
{
    public class BadAttachmentException : TypeBasedMessageException
    {
        internal BadAttachmentException(string message, Type type) : base(message, type)
        {
        }

        public static BadAttachmentException ForType<T>(string message)
        {
            return new BadAttachmentException(message, typeof (T));
        }
    }
}
