using System;
using Pipes.Abstraction;
using Pipes.Interfaces;

namespace Pipes.Types
{
    public class PipelineException<TContext> : Exception where TContext : class, IOperationContext
    {
        public IPipelineMessage<TContext> PipelineMessage { get; private set; }
        
        private readonly Pipeline<TContext> _pipeline;

        public PipelineException(Pipeline<TContext> pipeline, IPipelineMessage<TContext> pipelineMessage, Exception exception) : base("Pipeline Exception", exception)
        {
            _pipeline = pipeline;
            PipelineMessage = pipelineMessage;
        }
    }
}
