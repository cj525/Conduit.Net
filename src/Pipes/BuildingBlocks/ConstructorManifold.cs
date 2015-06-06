using System;
using Pipes.Abstraction;
using Pipes.Exceptions;
using Pipes.Interfaces;
using Pipes.Stubs;

namespace Pipes.BuildingBlocks
{
    internal class ConstructorManifold<T> : BuildingBlock, IPipelineConstructorMany<T>, IPipelineConstructorManyInitializer<T> where T : class
    {
        private ConstructorManifoldStub<T> _constructorManifold;

        private bool _blackhole = true;

        public ConstructorManifold(PipelineComponent component) : base(component)
        {
        }

        public ConstructorManifold(Pipeline pipeline) : base(pipeline)
        {
        }

        protected override void AttachPipeline(Pipeline pipeline)
        {
            if (_blackhole)
                throw new NotAttachedException("ConstructorMany is nonfunctional. (ctor without container)");

            // Create tx socket
            pipeline.AddCtorManifold(_constructorManifold);
        }


        public IPipelineConstructorManyInitializer<T> Into(ref Stub proxy)
        {
            _blackhole = false;
            if (proxy != default(Stub))
            {
                throw ProxyAlreadyAssignedException.Exception<T>("Attempted to reuse multi-construction stub.");
            }

            _constructorManifold = new ConstructorManifoldStub<T>(Component) {IsSupportClass = true};
            proxy = _constructorManifold;

            return this;
        }

        public IPipelineConstructorMany<T> Using(Func<T> ctor)
        {
            _blackhole = false;
            if (ctor == default(Func<T>))
            {
                _constructorManifold = new ConstructorManifoldStub<T>(Component);
            }

            _constructorManifold.Add(new ConstructorStub<T>(Component, ctor));

            return this;
        }

        public void WhichAreInitializedWith(Action<T> init)
        {
            _constructorManifold.SetInitializer(init);
        }
    }
}