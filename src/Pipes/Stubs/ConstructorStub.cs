using System;
using Pipes.Abstraction;

namespace Pipes.Stubs
{
    public class ConstructorStub<T> : Stub where T : PipelineComponent
    {
        private readonly Lazy<T> _instance;
        private readonly Func<T> _ctor;

        public ConstructorStub(PipelineComponent component, Func<T> ctor) : base(component, typeof(T)) 
        {
            _ctor = ctor;
            _instance = new Lazy<T>(ctor);
        }

        internal Func<T> Ctor { get { return _ctor; } }

        internal override void AttachTo(Pipeline pipeline)
        {
            pipeline.AttachComponent(_instance.Value);

            base.AttachTo(pipeline);
        }
    }
}