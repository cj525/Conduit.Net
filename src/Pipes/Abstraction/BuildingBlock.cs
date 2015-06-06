namespace Pipes.Abstraction
{
    public abstract class BuildingBlock
    {
        protected readonly PipelineComponent Component;

        protected BuildingBlock(PipelineComponent component)
        {
            Component = component;

            component.OnAttach(AttachPipeline);
        }

        protected BuildingBlock(Pipeline pipeline)
        {
            pipeline.OnAttach(AttachPipeline);
        }

        protected abstract void AttachPipeline(Pipeline pipeline);
    }
}