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
        where TContext : class
    {
        private readonly InvocationStub<TData,TContext> _proxy;
        private bool _blackhole = true;

        //public Invocation(PipelineComponent component, ref Action<T> trigger) : base(component)
        //{
        //    _proxy = new InvocationStub<T>(component);
        //    _proxy.GetTrigger(ref trigger);
        //}

        //public Invocation(PipelineComponent component, ref Func<T, Task> trigger) : base(component)
        //{
        //    _proxy = new InvocationStub<T>(component);
        //    _proxy.GetAsyncTrigger(ref trigger);
        //}

        public Invocation(Pipeline<TContext> pipeline, ref Action<TData,TContext> trigger) : base(pipeline)
        {
            _proxy = new InvocationStub<TData, TContext>();
            _proxy.GetTrigger(ref trigger);
        }

        public Invocation(Pipeline<TContext> pipeline, ref Func<TData, TContext, Task> trigger) : base(pipeline)
        {
            _proxy = new InvocationStub<TData, TContext>();
            _proxy.GetAsyncTrigger(ref trigger);
        }

        protected override void AttachPipeline(Pipeline<TContext> pipeline)
        {
            if (_blackhole)
                throw new NotAttachedException("Invocation is black-hole. (no receiver)");

            // Create ix socket
            pipeline.AddInvocation(_proxy);
        }

        public void WhichTransmitsTo(Stub<TContext> target)
        {
            _blackhole = false;
            _proxy.SetTarget(target);
        }
    }
}