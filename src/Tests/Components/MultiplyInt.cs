using Pipes.Abstraction;
using Pipes.Interfaces;
using Pipes.Tests.Simple;
using Pipes.Types;

namespace Pipes.Tests.Components
{
    class MultiplyInt : PipelineComponent
    {
        public int Value { get; set; }

        protected override void Describe(IPipelineComponentBuilder<OperationContext> thisComponent)
        {
            thisComponent
                .Receives<IntValue>()
                .WhichCalls(MultiplyTimesTwo);

            thisComponent
                .Emits<IntValue>();
        }

        private void MultiplyTimesTwo(IPipelineMessage<IntValue, OperationContext> message)
        {
            Emit(message, new IntValue {Value = message.Data.Value*Value});
        }
    }
}
