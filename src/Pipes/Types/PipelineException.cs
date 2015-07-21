using System;
using Pipes.Abstraction;
using Pipes.Interfaces;

namespace Pipes.Types
{
    public class PipelineException<TContext> : Exception where TContext : class
    {
        public IPipelineMessage<TContext> PipelineMessage { get; private set; }
        
        private readonly Pipeline<TContext> _pipeline;

        public PipelineException(Pipeline<TContext> pipeline, Exception exception, IPipelineMessage<TContext> pipelineMessage) : base("Pipeline Exception", exception)
        {
            _pipeline = pipeline;
            PipelineMessage = pipelineMessage;
        }

        // TODO: Consider deleting this
        public void Emit<T>(T data, TContext context = default(TContext)) where T : class
        {
            var source = PipelineMessage.Sender;
            var message = new PipelineMessage<T,TContext>(_pipeline, source, data, context, PipelineMessage);
            _pipeline.EmitMessage(message);
        }
    }
}
