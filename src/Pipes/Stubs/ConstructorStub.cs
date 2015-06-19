using System;
using Pipes.Abstraction;
using Pipes.Interfaces;

namespace Pipes.Stubs
{
    public class ConstructorStub<TComponent, TScope> : Stub<TScope> where TComponent : IPipelineComponent<TScope>
    {
        private readonly Lazy<TComponent> _instance;

        public ConstructorStub(IPipelineComponent<TScope> component, Func<TComponent> ctor) : base(component, typeof(TComponent)) 
        {
            _instance = new Lazy<TComponent>(ctor);
        }

        internal override void AttachTo(Pipeline<TScope> pipeline)
        {
            pipeline.AttachComponent(_instance.Value);

            base.AttachTo(pipeline);
        }
    }
}