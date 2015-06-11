using System;
using System.Linq;
using Pipes.Abstraction;
using Pipes.Extensions;

namespace Pipes.Stubs
{
    public abstract class ConstructorManifoldStub : Stub
    {
        protected ConstructorManifoldStub(PipelineComponent component, Type type) : base(component, type)
        {
            
        }
    }
    public class ConstructorManifoldStub<T> : ConstructorManifoldStub where T : PipelineComponent
    {
        private readonly ConstructorStub<T>[] _contents;

        public ConstructorManifoldStub(PipelineComponent component, int count, Func<T> ctor ) : base(component,typeof(T))
        {
            _contents = Enumerable
                .Range(0, count)
                .Select(_ => new ConstructorStub<T>(component, ctor))
                .ToArray();
        }

        internal override void AttachTo(Pipeline pipeline)
        {
            _contents.Apply(ctor => ctor.AttachTo(pipeline));

            base.AttachTo(pipeline);
        }
    }
}