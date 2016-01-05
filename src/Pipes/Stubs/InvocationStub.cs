using System;
using System.Threading.Tasks;
using Pipes.Abstraction;
using Pipes.Interfaces;
using Pipes.Types;

namespace Pipes.Stubs
{
    public abstract class InvocationStub<TContext> : Stub<TContext> where TContext : class, IOperationContext
    {
        internal Stub<TContext> Target;

        protected InvocationStub(Type containedType)
            : base(null, containedType)
        {
            
        }
    }

    public class InvocationStub<TData, TContext> : InvocationStub<TContext>
        where TData : class
        where TContext : class, IOperationContext
    {

        public InvocationStub() : base(typeof(TData))
        {
        }

        private async Task Invoke(TData data, object meta = null) 
        {
            var message = new PipelineMessage<TData,TContext>(Pipeline, Component, data, Pipeline.ConstructContext(), null, meta);
            await Pipeline.RouteMessage(message);
        }

        // ReSharper disable once RedundantAssignment
        internal void GetInvocation(ref Action<TData> trigger)
        {
            trigger = (data) => Invoke(data).Wait();
        }

        // ReSharper disable once RedundantAssignment
        internal void GetInvocation(ref Action<TData, object> trigger)
        {
            trigger = (data, meta) => Invoke(data, meta).Wait();
        }

        // ReSharper disable once RedundantAssignment
        internal void GetAsyncTrigger(ref Func<TData, Task> trigger)
        {
            trigger = (data) => Invoke(data);
        }
        // ReSharper disable once RedundantAssignment
        internal void GetAsyncTrigger(ref Func<TData, object, Task> trigger)
        {
            trigger = Invoke;
        }

        internal void SetTarget(Stub<TContext> target)
        {
            Target = target;
        }
    }
}