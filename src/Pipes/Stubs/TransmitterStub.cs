using System;
using System.Threading.Tasks;
using Pipes.Abstraction;
using Pipes.Interfaces;

namespace Pipes.Stubs
{
    public class TransmitterStub<TContext> : Stub<TContext> where TContext : class
    {
        protected TransmitterStub(IPipelineComponent<TContext> component, Type containedType) : base(component,containedType)
        {
        }
    }

    public class TransmitterStub<TComponent, TContext> : TransmitterStub<TContext>
        where TComponent : class
        where TContext : class
    {
        public TransmitterStub(IPipelineComponent<TContext> component): base(component, typeof(TComponent))
        {
        }


        //public async Task Emit(TComponent data, TContext context)
        //{
        //    await Pipeline.EmitAsync(Component, data, context);
        //}
    }
}