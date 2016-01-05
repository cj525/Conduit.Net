using System;
using System.Threading.Tasks;
using Pipes.Abstraction;
using Pipes.Exceptions;
using Pipes.Interfaces;
using Pipes.Stubs;

namespace Pipes.BuildingBlocks
{
    internal class Invocation<TData, TContext> : BuildingBlock<TContext>, IPipelineInvocation<TContext>
        where TData : class
        where TContext : class, IOperationContext
    {
        private readonly InvocationStub<TData,TContext> _proxy;
        private bool _blackhole = true;

        public Invocation(Pipeline<TContext> pipeline, ref Action<TData, object> trigger) : base(pipeline)
        {
            _proxy = new InvocationStub<TData, TContext>();
            _proxy.GetInvocation(ref trigger);
        }
        public Invocation(Pipeline<TContext> pipeline, ref Action<TData> trigger) : base(pipeline)
        {
            _proxy = new InvocationStub<TData, TContext>();
            _proxy.GetInvocation(ref trigger);
        }

        public Invocation(Pipeline<TContext> pipeline, ref Func<TData, object, Task> trigger) : base(pipeline)
        {
            _proxy = new InvocationStub<TData, TContext>();
            _proxy.GetAsyncTrigger(ref trigger);
        }

        public Invocation(Pipeline<TContext> pipeline, ref Func<TData, Task> trigger) : base(pipeline)
        {
            _proxy = new InvocationStub<TData, TContext>();
            _proxy.GetAsyncTrigger(ref trigger);
        }

        protected override void AttachPipeline(Pipeline<TContext> pipeline)
        {
            if (_blackhole)
                throw new NotAttachedException("Invocation is black-hole. (no receiver)");

            pipeline.AddInvocation(_proxy);
        }

        public void WhichTransmitsTo(Stub<TContext> target)
        {
            _blackhole = false;
            _proxy.SetTarget(target);
        }
    }
}