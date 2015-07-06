using System;
using System.Linq;
using Pipes.Abstraction;
using Pipes.Extensions;
using Pipes.Interfaces;

namespace Pipes.Stubs
{
    public abstract class ConstructorManifoldStub<TContext> : Stub<TContext> where TContext : class
    {
        protected ConstructorManifoldStub(IPipelineComponent<TContext> component, Type type) : base(component, type)
        {
            
        }
    }
    public class ConstructorManifoldStub<TComponent, TContext> : ConstructorManifoldStub<TContext>
        where TComponent : IPipelineComponent<TContext>
        where TContext : class
    {
        private readonly ConstructorStub<TComponent,TContext>[] _contents;

        public ConstructorManifoldStub(IPipelineComponent<TContext> component, int count, Func<TComponent> ctor ) : base(component,typeof(TComponent))
        {
            _contents = Enumerable
                .Range(0, count)
                .Select(_ => new ConstructorStub<TComponent,TContext>(component, ctor))
                .ToArray();
        }

        internal override void AttachTo(Pipeline<TContext> pipeline)
        {
            _contents.Apply(ctor => ctor.AttachTo(pipeline));

            base.AttachTo(pipeline);
        }
    }
}