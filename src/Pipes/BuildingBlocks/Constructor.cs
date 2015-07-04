using System;
using Pipes.Abstraction;
using Pipes.Exceptions;
using Pipes.Interfaces;
using Pipes.Stubs;

namespace Pipes.BuildingBlocks
{
    internal class Constructor<TComponent, TContext> : BuildingBlock<TContext>, IPipelineConstructor<TContext> where TComponent : IPipelineComponent<TContext>
    {
        private readonly Func<TComponent> _ctor;

        private ConstructorStub<TComponent,TContext> _constructor;

        private bool _blackhole = true;

        public Constructor(Pipeline<TContext> pipeline, Func<TComponent> ctor) : base(pipeline)
        {
            _ctor = ctor;
        }

        protected override void AttachPipeline(Pipeline<TContext> pipeline)
        {
            if (_blackhole)
                throw new NotAttachedException("Constructor is nonfunctional. (ctor without container)");

            pipeline.AddCtor(_constructor);
        }

        public void Into(ref Stub<TContext> proxy)
        {
            _blackhole = false;
            if (proxy != default(Stub<TContext>))
            {
                throw ProxyAlreadyAssignedException.ForType<TComponent>("Attempted to reuse construction stub.");
            }

            _constructor = new ConstructorStub<TComponent,TContext>(Component, _ctor);
            proxy = _constructor;
        }
    }
}