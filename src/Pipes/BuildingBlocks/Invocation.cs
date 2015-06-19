using System;
using System.Threading.Tasks;
using Pipes.Abstraction;
using Pipes.Exceptions;
using Pipes.Interfaces;
using Pipes.Stubs;

namespace Pipes.BuildingBlocks
{
    internal class Invocation<TData, TScope> : BuildingBlock<TScope>, IPipelineInvocation<TScope> where TData : class
    {
        private readonly InvocationStub<TData,TScope> _proxy;
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

        public Invocation(Pipeline<TScope> pipeline, ref Action<TData,TScope> trigger) : base(pipeline)
        {
            _proxy = new InvocationStub<TData, TScope>();
            _proxy.GetTrigger(ref trigger);
        }

        public Invocation(Pipeline<TScope> pipeline, ref Func<TData, TScope, Task> trigger) : base(pipeline)
        {
            _proxy = new InvocationStub<TData, TScope>();
            _proxy.GetAsyncTrigger(ref trigger);
        }

        protected override void AttachPipeline(Pipeline<TScope> pipeline)
        {
            if (_blackhole)
                throw new NotAttachedException("Invocation is black-hole. (no receiver)");

            // Create ix socket
            pipeline.AddInvocation(_proxy);
        }

        public void WhichTransmitsTo(Stub<TScope> target)
        {
            _blackhole = false;
            _proxy.SetTarget(target);
        }
    }
}