using System;
using Pipes.Abstraction;
using Pipes.Interfaces;

namespace Pipes.Types
{
    public class PipelineException<TScope> : Exception 
    {
        public Exception Exception { get; private set; }

        public IPipelineMessage<TScope> PipelineMessage { get; private set; }
        
        public TScope Scope { get; private set; }

        private readonly Pipeline<TScope> _pipeline;

        public PipelineException(Pipeline<TScope> pipeline, Exception exception, TScope scope, IPipelineMessage<TScope> pipelineMessage = null)
        {
            _pipeline = pipeline;
            PipelineMessage = pipelineMessage;
            Exception = exception;
            Scope = scope;
        }

        public void TerminatePipeline()
        {
            _pipeline.Terminate();
        }

        public void Emit<T>(T data, TScope scope = default(TScope)) where T : class
        {
            var source = PipelineMessage == null ? null : PipelineMessage.Sender;
            if (scope.Equals(default(TScope)))
            {
                scope = Scope;
            }
            var message = new PipelineMessage<T,TScope>(_pipeline, source, data, scope, PipelineMessage);
            _pipeline.EmitMessage(message);
        }
    }
}
