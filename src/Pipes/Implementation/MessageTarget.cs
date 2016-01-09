using System;
using System.Data.SqlTypes;
using System.Reflection;
using System.Threading.Tasks;
using Pipes.Abstraction;
using Pipes.Interfaces;
using Pipes.Stubs;
using Pipes.Types;

namespace Pipes.Implementation
{
    internal class MessageTarget<TData, TContext> : Target
        where TData : class
        where TContext : OperationContext
    {
        //private Func<IPipelineMessage<TData, TContext>, Task> _bridge;

        internal void Attach(ReceiverStub<TData, TContext> rx)
        {
            rx.Target = this;
            //ParameterType = typeof(TData);
        }

        internal void Set(Action action)
        {
            MethodInfo = action.Method;
            Instance = action.Target;
            IsTrigger = true;
        }

        internal void Set(Action<TData> action)
        {
            MethodInfo = action.Method;
            Instance = action.Target;
            IsUnwrapped = true;
        }

        internal void Set(Action<IPipelineMessage<TData, TContext>> action)
        {
            MethodInfo = action.Method;
            Instance = action.Target;
        }

        internal void Set(Func<Task> asyncAction)
        {
            MethodInfo = asyncAction.Method;
            Instance = asyncAction.Target;
            IsTrigger = true;
            ReturnsTask = true;
        }

        internal void Set(Func<TData, Task> asyncAction)
        {
            MethodInfo = asyncAction.Method;
            Instance = asyncAction.Target;
            IsUnwrapped = true;
            ReturnsTask = true;
        }

        internal void Set(Func<IPipelineMessage<TData, TContext>, Task> asyncAction)
        {
            Instance = asyncAction.Target;
            MethodInfo = asyncAction.Method;
            ReturnsTask = true;
        }
    }

}
