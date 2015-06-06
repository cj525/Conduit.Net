using System;
using System.Threading.Tasks;
using Pipes.Abstraction;
using Pipes.Exceptions;
using Pipes.Interfaces;
using Pipes.Stubs;

namespace Pipes.BuildingBlocks
{
    internal class Invocation<T> : BuildingBlock, IPipelineInvocation<T> where T : class
    {
        private readonly InvocationStub<T> _proxy;
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

        public Invocation(Pipeline pipeline, ref Action<T> trigger) : base(pipeline)
        {
            _proxy = new InvocationStub<T>();
            _proxy.GetTrigger(ref trigger);
        }

        public Invocation(Pipeline pipeline, ref Func<T, Task> trigger) : base(pipeline)
        {
            _proxy = new InvocationStub<T>();
            _proxy.GetAsyncTrigger(ref trigger);
        }

        protected override void AttachPipeline(Pipeline pipeline)
        {
            if (_blackhole)
                throw new NotAttachedException("Invocation is black-hole. (no receiver)");

            // Create ix socket
            pipeline.AddInvocation(_proxy);
        }

        public void WhichTransmitsTo(Stub target)
        {
            _blackhole = false;
            _proxy.SetTarget(target);
        }
    }
}