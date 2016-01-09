using System;
using System.Threading.Tasks;
using Pipes.Abstraction;
using Pipes.Interfaces;
using Pipes.Types;

namespace Pipes.Stubs
{
    public class TransmitterStub<TContext> : Stub<TContext> where TContext : OperationContext
    {
        protected TransmitterStub(IPipelineComponent<TContext> component, Type containedType) : base(component,containedType)
        {
        }
    }

    public class TransmitterStub<TComponent, TContext> : TransmitterStub<TContext>
        where TComponent : class
        where TContext : OperationContext
    {
        public TransmitterStub(IPipelineComponent<TContext> component, Type containedType): base(component, containedType)
        {
        }
    }
}