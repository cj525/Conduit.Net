using System;
using Pipes.Abstraction;
using Pipes.Interfaces;
using Pipes.Tests.Simple;

namespace Pipes.Tests.Components
{
    class ToString<T> : PipelineComponent where T : class
    {
        private readonly string _format;

        public ToString(string format)
        {
            _format = format;
        }

        protected override void Describe(IPipelineComponentBuilder<IOperationContext> thisComponent)
        {
            thisComponent
                .Receives<T>()
                .WhichCalls(Format);

            thisComponent
                .Emits<StringValue>();
        }

        private void Format(IPipelineMessage<T, IOperationContext> message)
        {
            Emit( message, new StringValue { Value = String.Format(_format,message.Data) } );
        }
    }
}
