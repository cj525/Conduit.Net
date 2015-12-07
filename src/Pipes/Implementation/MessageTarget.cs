using System;
using System.Threading.Tasks;
using Pipes.Abstraction;
using Pipes.Interfaces;
using Pipes.Stubs;

namespace Pipes.Implementation
{
    internal class MessageTarget<TData, TContext> : Target
        where TData : class
        where TContext : class, IOperationContext
    {
        private Func<IPipelineMessage<TData, TContext>, Task> _bridge;

        internal void Attach(ReceiverStub<TData, TContext> rx)
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

        internal void Set(Action<TData> action)
        {
            _bridge = msg =>
            {
                action(msg.Data);
                return EmptyTask;
            };
        }

        internal void Set(Action<IPipelineMessage<TData, TContext>> action)
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

        internal void Set(Func<TData, Task> asyncAction)
        {
            _bridge = msg => asyncAction(msg.Data);
        }

        internal void Set(Func<IPipelineMessage<TData, TContext>, Task> asyncAction)
        {
            _bridge = asyncAction;
        }
    }

}
