using System;
using System.Threading.Tasks;
using Pipes.Abstraction;
using Pipes.Interfaces;

namespace Pipes.Stubs
{
    public class TransmitterStub<TContext> : Stub<TContext>
    {
        protected TransmitterStub(IPipelineComponent<TContext> component, Type containedType) : base(component,containedType)
        {
        }
    }

    public class TransmitterStub<T,TContext> : TransmitterStub<TContext> where T : class
    {
        public TransmitterStub(IPipelineComponent<TContext> component): base(component, typeof(T))
        {
        }


        public async Task Emit(T data, TContext context)
        {
            await Pipeline.EmitAsync(Component, data, context);
        }


    }
}