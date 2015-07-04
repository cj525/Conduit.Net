using System;
using Pipes.Abstraction;
using Pipes.Interfaces;

namespace Pipes.Types
{
    public class PipelineException<TContext> : Exception 
    {
        public Exception Exception { get; private set; }

        public IPipelineMessage<TContext> PipelineMessage { get; private set; }
        
        public TContext Context { get; private set; }

        private readonly Pipeline<TContext> _pipeline;

        public PipelineException(Pipeline<TContext> pipeline, Exception exception, TContext context, IPipelineMessage<TContext> pipelineMessage = null)
        {
            _pipeline = pipeline;
            PipelineMessage = pipelineMessage;
            Exception = exception;
            Context = context;
        }

        public void TerminatePipeline()
        {
            _pipeline.Terminate();
        }

        public void Emit<T>(T data, TContext context = default(TContext)) where T : class
        {
            var source = PipelineMessage == null ? null : PipelineMessage.Sender;
            if (context.Equals(default(TContext)))
            {
                context = Context;
            }
            var message = new PipelineMessage<T,TContext>(_pipeline, source, data, context, PipelineMessage);
            _pipeline.EmitMessage(message);
        }
    }
}
