using System;
using Pipes.Abstraction;
using Pipes.Exceptions;
using Pipes.Interfaces;
using Pipes.Stubs;

namespace Pipes.BuildingBlocks
{
    internal class ConstructorManifold<TContext> : IPipelineConstructorMany<TContext>
    {
        private readonly int _count;
        private readonly Pipeline<TContext> _pipeline;

        public ConstructorManifold(Pipeline<TContext> pipeline, int count)
        {
            _pipeline = pipeline;
            _count = count;
        }

        public IPipelineConstructorManyTarget<TContext> Using<TComponent>(Func<TComponent> ctor) where TComponent : IPipelineComponent<TContext>
        {
            return new ConstructorManifold<TComponent,TContext>(_pipeline,_count,ctor);
        }
    }
    internal class ConstructorManifold<TComponent,TContext> : BuildingBlock<TContext>, IPipelineConstructorManyTarget<TContext> where TComponent : IPipelineComponent<TContext>
    {
        private readonly ConstructorManifoldStub<TComponent,TContext> _constructorManifold;
        private bool _blackhole = true;

        public ConstructorManifold(Pipeline<TContext> pipeline, int count, Func<TComponent> ctor)
            : base(pipeline)
        {
            _constructorManifold = new ConstructorManifoldStub<TComponent,TContext>(Component, count, ctor);
        }

        protected override void AttachPipeline(Pipeline<TContext> pipeline)
        {
            if (_blackhole)
                throw new NotAttachedException("ConstructorManifold is nonfunctional. (ctor without container)");
        
            pipeline.AddCtorManifold(_constructorManifold);
        }


        public void Into(ref Stub<TContext> proxy)
        {
            _blackhole = false;
            if (proxy != default(Stub<TContext>))
            {
                throw ProxyAlreadyAssignedException.ForType<TComponent>("Attempted to reuse multi-construction stub.");
            }

            proxy = _constructorManifold;
        }

        public IPipelineConstructorManyTarget<TContext> Using(Func<TComponent> ctor)
        {
            _blackhole = false;

            return this;
        }
    }
}