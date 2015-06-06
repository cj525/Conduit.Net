using System;

namespace Pipes.Exceptions.Abstraction
{
    public abstract class TypeBasedMessageException : Exception
    {
        // Again, resharper, listen, just because it's not used NOW, doesn't mean it won't be consumed elsewhere
        // I mean, come on, it's an Exception.  Get your code together, Resharper!
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        // ReSharper disable once MemberCanBePrivate.Global
        public Type OnType { get; private set; }

        protected TypeBasedMessageException(string message, Type type) : base(String.Concat("[", type.Name, "] ", message))
        {
            OnType = type;
        }
    }
}
