using Pipes.Abstraction;
using Pipes.Interfaces;
using Pipes.Stubs;

namespace Pipes.BuildingBlocks
{
    internal class Transmitter<T,TScope> : BuildingBlock<TScope> where T : class
    {
        private readonly TransmitterStub<T,TScope> _tx;

        private Transmitter(IPipelineComponent<TScope> component) : base(component)
        {
            _tx = new TransmitterStub<T,TScope>(component);
        }

        protected override void AttachPipeline(Pipeline<TScope> pipeline)
        {
            // Create tx socket
            pipeline.AddTx(_tx, Component);
        }

        internal static void AttachTo(IPipelineComponent<TScope> component)
        {
            // Although this is never used, the base class hooks up this up to the component
            // And that is needed to ensure routable messages from pipeline describers
            // ReSharper disable once ObjectCreationAsStatement
            new Transmitter<T,TScope>(component);
        }
    }
}