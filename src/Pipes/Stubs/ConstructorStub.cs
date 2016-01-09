using System;
using Pipes.Abstraction;
using Pipes.Interfaces;
using Pipes.Types;

namespace Pipes.Stubs
{
    public class ConstructorStub<TComponent, TContext> : Stub<TContext>, IPipelineConstructorStub<TContext>
        where TComponent : IPipelineComponent<TContext>
        where TContext : OperationContext
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

        IPipelineComponent<TContext> IPipelineConstructorStub<TContext>.Construct()
        {
            return _ctor();
        }
    }
}