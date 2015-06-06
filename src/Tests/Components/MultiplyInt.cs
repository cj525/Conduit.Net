using Pipes.Abstraction;
using Pipes.Interfaces;

namespace Pipes.Tests.Components
{
    class MultiplyInt : PipelineComponent
    {
        public int Value { get; set; }

        protected override void Describe(IPipelineComponentBuilder thisComponent)
        {
            thisComponent
                .Receives<object>()
                .WhichUnwrapsAndCalls(MultiplyTimesTwo);

            thisComponent
                .Emits<object>();
        }

        private void MultiplyTimesTwo(object x)
        {
            Emit((object)((int)x * Value));
        }
    }
}
