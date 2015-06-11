using System;
using Pipes.Abstraction;
using Pipes.Exceptions;
using Pipes.Interfaces;
using Pipes.Stubs;

namespace Pipes.BuildingBlocks
{
    internal class ConstructorManifold<T> : BuildingBlock, IPipelineConstructorMany<T>, IPipelineConstructorManyTarget<T> where T : PipelineComponent
    {
        private readonly ConstructorManifoldStub<T> _constructorManifold;
        private bool _blackhole = true;

        public ConstructorManifold(Pipeline pipeline, int count) : base(pipeline)
        {
            _constructorManifold = new ConstructorManifoldStub<T>(Component,count);
        }

        protected override void AttachPipeline(Pipeline pipeline)
        {
            if (_blackhole)
                throw new NotAttachedException("ConstructorManifold is nonfunctional. (ctor without container)");
        
            pipeline.AddCtorManifold(_constructorManifold);
        }


        public void Into(ref Stub proxy)
        {
            _blackhole = false;
            if (proxy != default(Stub))
            {
                throw ProxyAlreadyAssignedException.ForType<T>("Attempted to reuse multi-construction stub.");
            }

            proxy = _constructorManifold;
        }

        public IPipelineConstructorManyTarget<T> Using(Func<T> ctor)
        {
            _blackhole = false;

            _constructorManifold.Add(new ConstructorStub<T>(Component, ctor));

            return this;
        }
    }
}