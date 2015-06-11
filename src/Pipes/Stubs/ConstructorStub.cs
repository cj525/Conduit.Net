using System;
using Pipes.Abstraction;

namespace Pipes.Stubs
{
    public class ConstructorStub<T> : Stub where T : PipelineComponent
    {
        private readonly Lazy<T> _instance;

        public ConstructorStub(PipelineComponent component, Func<T> ctor) : base(component, typeof(T)) 
        {
            _instance = new Lazy<T>(ctor);
        }

        internal override void AttachTo(Pipeline pipeline)
        {
            pipeline.AttachComponent(_instance.Value);

            base.AttachTo(pipeline);
        }
    }
}