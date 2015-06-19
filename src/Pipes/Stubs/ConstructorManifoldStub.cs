using System;
using System.Linq;
using Pipes.Abstraction;
using Pipes.Extensions;
using Pipes.Interfaces;

namespace Pipes.Stubs
{
    public abstract class ConstructorManifoldStub<TScope> : Stub<TScope>
    {
        protected ConstructorManifoldStub(IPipelineComponent<TScope> component, Type type) : base(component, type)
        {
            
        }
    }
    public class ConstructorManifoldStub<TComponent, TScope> : ConstructorManifoldStub<TScope> where TComponent : IPipelineComponent<TScope>
    {
        private readonly ConstructorStub<TComponent,TScope>[] _contents;

        public ConstructorManifoldStub(IPipelineComponent<TScope> component, int count, Func<TComponent> ctor ) : base(component,typeof(TComponent))
        {
            _contents = Enumerable
                .Range(0, count)
                .Select(_ => new ConstructorStub<TComponent,TScope>(component, ctor))
                .ToArray();
        }

        internal override void AttachTo(Pipeline<TScope> pipeline)
        {
            _contents.Apply(ctor => ctor.AttachTo(pipeline));

            base.AttachTo(pipeline);
        }
    }
}