using System;
using System.Threading.Tasks;
using Pipes.Abstraction;
using Pipes.Interfaces;
using Pipes.Stubs;

namespace Pipes.Implementation
{
    internal class MessageTarget<T,TScope> : Target where T : class
    {
        private Func<IPipelineMessage<T, TScope>, Task> _bridge;

        internal void Attach(ReceiverStub<T, TScope> rx)
        {
            rx.Calls(_bridge);
        }

        internal void Set(Action action)
        {
            _bridge = msg =>
            {
                action();
                return EmptyTask;
            };
        }

        internal void Set(Action<T> action)
        {
            _bridge = msg =>
            {
                action(msg.Data);
                return EmptyTask;
            };
        }

        internal void Set(Action<IPipelineMessage<T, TScope>> action)
        {
            _bridge = msg =>
            {
                action(msg);
                return EmptyTask;
            };
        }

        internal void Set(Func<Task> asyncAction)
        {
            _bridge = msg => asyncAction();
        }

        internal void Set(Func<T, Task> asyncAction)
        {
            _bridge = msg => asyncAction(msg.Data);
        }

        internal void Set(Func<IPipelineMessage<T, TScope>, Task> asyncAction)
        {
            _bridge = asyncAction;
        }
    }

}
