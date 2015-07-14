using System;
using Pipes.Abstraction;
using Pipes.Interfaces;

namespace Pipes.Stubs
{
    public class ConstructorStub<TComponent, TContext> : Stub<TContext>
        where TComponent : IPipelineComponent<TContext>
        where TContext : class
    {
        private readonly Func<TComponent> _ctor;

        public ConstructorStub(IPipelineComponent<TContext> component, Func<TComponent> ctor) : base(component, typeof(TComponent)) 
        {
            _ctor = ctor;
        }

        internal override void AttachTo(Pipeline<TContext> pipeline)
        {
            var instance = _ctor();

            pipeline.AttachComponent(instance);

            base.AttachTo(pipeline);
        }
    }
}