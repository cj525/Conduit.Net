using System;
using System.Threading.Tasks;
using Pipes.Abstraction;

namespace Pipes.Stubs
{
    public class TransmitterStub : Stub
    {
        protected TransmitterStub(PipelineComponent component, Type containedType) : base(component,containedType)
        {
        }
    }

    public class TransmitterStub<T> : TransmitterStub where T : class
    {
        public TransmitterStub(PipelineComponent component): base(component,typeof(T))
        {
        }


        public Task Emit(T data)
        {
            return Pipeline.EmitAsync(Component, data);
        }


    }
}