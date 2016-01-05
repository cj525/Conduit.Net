using Pipes.Abstraction;
using Pipes.Interfaces;
using Pipes.Tests.Simple;

namespace Pipes.Tests.Components
{
    class MultiplyInt : PipelineComponent
    {
        public int Value { get; set; }

        protected override void Describe(IPipelineComponentBuilder<IOperationContext> thisComponent)
        {
            thisComponent
                .Receives<IntValue>()
                .WhichCalls(MultiplyTimesTwo);

            thisComponent
                .Emits<IntValue>();
        }

        private void MultiplyTimesTwo(IPipelineMessage<IntValue, IOperationContext> message)
        {
            Emit(message, new IntValue {Value = message.Data.Value*Value});
        }
    }
}
