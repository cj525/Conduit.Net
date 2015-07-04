using System;
using Pipes.Abstraction;
using Pipes.Interfaces;

namespace Pipes.Stubs
{
    public class ConstructorStub<TComponent, TContext> : Stub<TContext> where TComponent : IPipelineComponent<TContext>
    {
        private readonly Lazy<TComponent> _instance;

        public ConstructorStub(IPipelineComponent<TContext> component, Func<TComponent> ctor) : base(component, typeof(TComponent)) 
        {
            _instance = new Lazy<TComponent>(ctor);
        }

        internal override void AttachTo(Pipeline<TContext> pipeline)
        {
            pipeline.AttachComponent(_instance.Value);

            base.AttachTo(pipeline);
        }
    }
}