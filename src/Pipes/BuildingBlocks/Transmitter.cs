using Pipes.Abstraction;
using Pipes.Interfaces;
using Pipes.Stubs;
using Pipes.Types;

namespace Pipes.BuildingBlocks
{
    /// <summary>
    /// Simply records the fact that the component in the transmits that type
    /// Staying with the pattern, this information is transferred to a stub
    /// </summary>
    /// <typeparam name="TData"></typeparam>
    /// <typeparam name="TContext"></typeparam>
    internal class Transmitter<TData, TContext> : BuildingBlock<TContext>
        where TData : class
        where TContext : OperationContext
    {
        private readonly TransmitterStub<TData,TContext> _tx;

        private Transmitter(IPipelineComponent<TContext> component) : base(component)
        {
            _tx = new TransmitterStub<TData,TContext>(component, typeof(TData));
        }

        protected override void AttachPipeline(Pipeline<TContext> pipeline)
        {
            // Create tx socket
            pipeline.AddTx(_tx);
        }

        internal static void AttachTo(IPipelineComponent<TContext> component)
        {
            // Although this is never used, the base class hooks up this up to the component
            // And that is needed to ensure routable messages from pipeline describers
            // ReSharper disable once ObjectCreationAsStatement
            new Transmitter<TData,TContext>(component);
        }
    }
}