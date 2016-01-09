using Pipes.Abstraction;
using Pipes.Interfaces;
using Pipes.Tests.Simple;
using Pipes.Types;

namespace Pipes.Tests.Components
{
    class AddInt : PipelineComponent
    {
        public int Value { get; set; }

        protected override void Describe(IPipelineComponentBuilder<OperationContext> thisComponent)
        {
            thisComponent
                .Receives<IntValue>()
                .WhichCalls(AddToTwo);

            thisComponent
                .Emits<IntValue>();
        }

        private void AddToTwo(IPipelineMessage<IntValue, OperationContext> message)
        {
            Emit(message, new IntValue {Value = message.Data.Value+Value});
        } 
    }
}
