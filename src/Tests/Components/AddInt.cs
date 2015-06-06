using Pipes.Abstraction;
using Pipes.Interfaces;

namespace Pipes.Tests.Components
{
    class AddInt : PipelineComponent
    {
        public int Value { get; set; }

        protected override void Describe(IPipelineComponentBuilder thisComponent)
        {
            thisComponent
                .Receives<object>()
                .WhichUnwrapsAndCalls(AddToTwo);

            thisComponent
                .Emits<object>();
        }

        private void AddToTwo(object x)
        {
            Emit((object)((int)x + Value));
        }

        public class AddedInt
        {
            public int Value { get; set; }

            public AddedInt(int value)
            {
                Value = value;
            }
        }
    }
}
