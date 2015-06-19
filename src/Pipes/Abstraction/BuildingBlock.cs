using Pipes.Interfaces;

namespace Pipes.Abstraction
{
    public abstract class BuildingBlock<TScope>
    {
        protected readonly IPipelineComponent<TScope> Component;

        protected BuildingBlock(IPipelineComponent<TScope> component)
        {
            Component = component;

            component.OnAttach(AttachPipeline);
        }

        protected BuildingBlock(Pipeline<TScope> pipeline)
        {
            pipeline.OnAttach(AttachPipeline);
        }

        protected abstract void AttachPipeline(Pipeline<TScope> pipeline);
    }
}