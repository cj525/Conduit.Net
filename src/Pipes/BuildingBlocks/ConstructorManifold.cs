using System;
using Pipes.Abstraction;
using Pipes.Exceptions;
using Pipes.Interfaces;
using Pipes.Stubs;

namespace Pipes.BuildingBlocks
{
    internal class ConstructorManifold<TScope> : IPipelineConstructorMany<TScope>
    {
        private readonly int _count;
        private readonly Pipeline<TScope> _pipeline;

        public ConstructorManifold(Pipeline<TScope> pipeline, int count)
        {
            _pipeline = pipeline;
            _count = count;
        }

        public IPipelineConstructorManyTarget<TComponent,TScope> Using<TComponent>(Func<TComponent> ctor) where TComponent : IPipelineComponent<TScope>
        {
            return new ConstructorManifold<TComponent,TScope>(_pipeline,_count,ctor);
        }
    }
    internal class ConstructorManifold<TComponent,TScope> : BuildingBlock<TScope>, IPipelineConstructorManyTarget<TComponent,TScope> where TComponent : IPipelineComponent<TScope>
    {
        private readonly ConstructorManifoldStub<TComponent,TScope> _constructorManifold;
        private bool _blackhole = true;

        public ConstructorManifold(Pipeline<TScope> pipeline, int count, Func<TComponent> ctor)
            : base(pipeline)
        {
            _constructorManifold = new ConstructorManifoldStub<TComponent,TScope>(Component, count, ctor);
        }

        protected override void AttachPipeline(Pipeline<TScope> pipeline)
        {
            if (_blackhole)
                throw new NotAttachedException("ConstructorManifold is nonfunctional. (ctor without container)");
        
            pipeline.AddCtorManifold(_constructorManifold);
        }


        public void Into(ref Stub<TScope> proxy)
        {
            _blackhole = false;
            if (proxy != default(Stub<TScope>))
            {
                throw ProxyAlreadyAssignedException.ForType<TComponent>("Attempted to reuse multi-construction stub.");
            }

            proxy = _constructorManifold;
        }

        public IPipelineConstructorManyTarget<TComponent,TScope> Using(Func<TComponent> ctor)
        {
            _blackhole = false;

            return this;
        }
    }
}