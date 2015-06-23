using System;
using System.Threading.Tasks;
using Pipes.Abstraction;
using Pipes.Interfaces;

namespace Pipes.Stubs
{
    public class TransmitterStub<TScope> : Stub<TScope>
    {
        protected TransmitterStub(IPipelineComponent<TScope> component, Type containedType) : base(component,containedType)
        {
        }
    }

    public class TransmitterStub<T,TScope> : TransmitterStub<TScope> where T : class
    {
        public TransmitterStub(IPipelineComponent<TScope> component): base(component, typeof(T))
        {
        }


        public async Task Emit(T data, TScope scope)
        {
            await Pipeline.EmitAsync(Component, data, scope);
        }


    }
}