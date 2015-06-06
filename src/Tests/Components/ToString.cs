using System;
using Pipes.Abstraction;
using Pipes.Interfaces;

namespace Pipes.Tests.Components
{
    class ToString<T> : PipelineComponent where T : class
    {
        private readonly string _format;

        public ToString(string format)
        {
            _format = format;
        }

        protected override void Describe(IPipelineComponentBuilder thisComponent)
        {
            thisComponent
                .Receives<T>()
                .WhichUnwrapsAndCalls(Format);

            thisComponent
                .Emits<string>();
        }

        private void Format(T value)
        {
            Emit(String.Format(_format,value));
        }
    }
}
