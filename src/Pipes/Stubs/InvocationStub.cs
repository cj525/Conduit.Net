using System;
using System.Threading.Tasks;
using Pipes.Abstraction;
using Pipes.BuildingBlocks;
using Pipes.Extensions;
using Pipes.Implementation;
using Pipes.Interfaces;
using Pipes.Types;

namespace Pipes.Stubs
{
    public abstract class InvocationStub<TContext> : Stub<TContext> where TContext : OperationContext
    {
        internal Stub<TContext> TargetStub;

        protected InvocationStub(Type containedType) : base(null, containedType)
        {
            
        }
    }

    public class InvocationStub<TData, TContext> : InvocationStub<TContext>
        where TData : class
        where TContext : OperationContext
    {

        public InvocationStub() : base(typeof(TData))
        {
        }

        private async Task Invoke(TData data, object meta = null)
        {
            // Create a context, overrides may be interested in the original data/meta 
            using (var context = Pipeline.ConstructContext(data, meta))
            {
                // Store components
                context.SetComponents(Pipeline.ConstructComponents());

                // Begin a message chain
                var message = new PipelineMessage<TData, TContext>(Pipeline, Component, data, context, null, meta);

                // Run the pipeline
                Pipeline.RouteMessage(message, TargetStub);

                // Wait for all messages to complete
                await context.WaitForCompletion();

                // Check for a throwable error
                if (context.UnhandledException != null)

                    // Throw
                    throw context.UnhandledException;
            }
        }

        internal void GetInvocation(ref Action<TData> trigger)
        {
            trigger = (data) => Invoke(data).Wait();
        }

        internal void GetInvocation(ref Action<TData, object> trigger)
        {
            trigger = (data, meta) => Invoke(data, meta).Wait();
        }

        internal void GetAsyncTrigger(ref Func<TData, Task> trigger)
        {
            trigger = (data) => Invoke(data);
        }
        internal void GetAsyncTrigger(ref Func<TData, object, Task> trigger)
        {
            trigger = Invoke;
        }

        internal void SetTargetStub(Stub<TContext> stub)
        {
            TargetStub = stub;
            Routes.Add(new Route<TContext>(null, stub, typeof (TData)));
        }
    }
}