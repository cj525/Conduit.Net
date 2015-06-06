using Pipes.Abstraction;
using Pipes.Stubs;

namespace Pipes.BuildingBlocks
{
    internal class Transmitter<T> : BuildingBlock where T : class
    {
        private readonly TransmitterStub<T> _tx;

        private Transmitter(PipelineComponent component) : base(component)
        {
            _tx = new TransmitterStub<T>(component);
        }

        protected override void AttachPipeline(Pipeline pipeline)
        {
            // Create tx socket
            pipeline.AddTx(_tx, Component);
        }

        internal static void AttachTo(PipelineComponent component)
        {
            // Although this is never used, the base class hooks up this up to the component
            // And that is needed to ensure routable messages from pipeline describers
            // ReSharper disable once ObjectCreationAsStatement
            new Transmitter<T>(component);
        }
    }
}