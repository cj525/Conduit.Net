using System;
using Pipes.Abstraction;
using Pipes.Exceptions;
using Pipes.Interfaces;
using Pipes.Stubs;

namespace Pipes.BuildingBlocks
{
    internal class Constructor<TComponent, TScope> : BuildingBlock<TScope>, IPipelineConstructor<TScope> where TComponent : IPipelineComponent<TScope>
    {
        private readonly Func<TComponent> _ctor;

        private ConstructorStub<TComponent,TScope> _constructor;

        private bool _blackhole = true;

        public Constructor(Pipeline<TScope> pipeline, Func<TComponent> ctor) : base(pipeline)
        {
            _ctor = ctor;
        }

        protected override void AttachPipeline(Pipeline<TScope> pipeline)
        {
            if (_blackhole)
                throw new NotAttachedException("Constructor is nonfunctional. (ctor without container)");

            pipeline.AddCtor(_constructor);
        }

        public void Into(ref Stub<TScope> proxy)
        {
            _blackhole = false;
            if (proxy != default(Stub<TScope>))
            {
                throw ProxyAlreadyAssignedException.ForType<TComponent>("Attempted to reuse construction stub.");
            }

            _constructor = new ConstructorStub<TComponent,TScope>(Component, _ctor);
            proxy = _constructor;
        }
    }
}