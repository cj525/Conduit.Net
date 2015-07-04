using Pipes.Abstraction;
using Pipes.Interfaces;
using Pipes.Stubs;

namespace Pipes.BuildingBlocks
{
    internal class Transmitter<T,TContext> : BuildingBlock<TContext> where T : class
    {
        private readonly TransmitterStub<T,TContext> _tx;

        private Transmitter(IPipelineComponent<TContext> component) : base(component)
        {
            _tx = new TransmitterStub<T,TContext>(component);
        }

        protected override void AttachPipeline(Pipeline<TContext> pipeline)
        {
            // Create tx socket
            pipeline.AddTx(_tx, Component);
        }

        internal static void AttachTo(IPipelineComponent<TContext> component)
        {
            // Although this is never used, the base class hooks up this up to the component
            // And that is needed to ensure routable messages from pipeline describers
            // ReSharper disable once ObjectCreationAsStatement
            new Transmitter<T,TContext>(component);
        }
    }
}