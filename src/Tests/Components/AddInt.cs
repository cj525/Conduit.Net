using Pipes.Abstraction;
using Pipes.Interfaces;
using Pipes.Tests.Simple;

namespace Pipes.Tests.Components
{
    class AddInt : PipelineComponent
    {
        public int Value { get; set; }

        protected override void Describe(IPipelineComponentBuilder<IOperationContext> thisComponent)
        {
            thisComponent
                .Receives<IntValue>()
                .WhichCalls(AddToTwo);

            thisComponent
                .Emits<IntValue>();
        }

        private void AddToTwo(IPipelineMessage<IntValue, IOperationContext> message)
        {
            Emit(message, new IntValue {Value = message.Data.Value*Value});
        } 
    }
}
