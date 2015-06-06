using System;
using Pipes.Abstraction;
using Pipes.Exceptions;
using Pipes.Interfaces;
using Pipes.Stubs;

namespace Pipes.BuildingBlocks
{
    internal class Constructor<T> : BuildingBlock, IPipelineConstructor<T> where T : class
    {
        private readonly Func<T> _ctor;

        private ConstructorStub<T> _constructor;

        private bool _blackhole = true;
        private bool _once;


        public Constructor(PipelineComponent component, Func<T> ctor) : base(component)
        {
            _ctor = ctor;
        }

        public Constructor(Pipeline pipeline, Func<T> ctor) : base(pipeline)
        {
            _ctor = ctor;
        }


        protected override void AttachPipeline(Pipeline pipeline)
        {
            if (_blackhole)
                throw new NotAttachedException("Constructor is nonfunctional. (ctor without container)");

            // Create tx socket
            pipeline.AddCtor(_constructor);
        }

        public void Once()
        {
            _once = true;

        }

        public void Into(ref Stub proxy)
        {
            _blackhole = false;
            if (proxy != default(Stub))
            {
                throw ProxyAlreadyAssignedException.Exception<T>("Attempted to reuse construction stub.");
            }

            _constructor = new ConstructorStub<T>(Component, _ctor);
            proxy = _constructor;
        }

        public IPipelineSupportConstructor<T> Using(ref Func<T> ctor)
        {
            _blackhole = false;
            if (ctor != default(Func<T>))
            {
                throw ProxyAlreadyAssignedException.Exception<T>("Attempted to reuse construction proxy.");
            }
            ctor = _ctor;
            _constructor = new ConstructorStub<T>(Component, ctor) { OncePerConstruct = _once, IsSupportClass = true };

            return this;
        }
    }
}