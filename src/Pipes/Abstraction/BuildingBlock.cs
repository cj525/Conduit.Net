using Pipes.Interfaces;
using Pipes.Types;

namespace Pipes.Abstraction
{
    public abstract class BuildingBlock<TContext> where TContext : OperationContext
    {
        protected readonly IPipelineComponent<TContext> Component;

        protected BuildingBlock(IPipelineComponent<TContext> component)
        {
            Component = component;

            component.OnAttach(AttachPipeline);
        }

        protected BuildingBlock(Pipeline<TContext> pipeline)
        {
            pipeline.OnAttach(AttachPipeline);
        }

        protected abstract void AttachPipeline(Pipeline<TContext> pipeline);
    }
}