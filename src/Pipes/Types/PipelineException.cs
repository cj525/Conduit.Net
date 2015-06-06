using System;
using Pipes.Abstraction;
using Pipes.Interfaces;

namespace Pipes.Types
{
    public class PipelineException : Exception
    {
        public readonly Exception Exception;
        public readonly IPipelineMessage PipelineMessage;

        private readonly Pipeline _pipeline;

        public PipelineException(Pipeline pipeline, Exception exception, IPipelineMessage pipelineMessage = null)
        {
            _pipeline = pipeline;
            PipelineMessage = pipelineMessage;
            Exception = exception;
        }

        public void TerminatePipeline()
        {
            _pipeline.Terminate();
        }

        public void Emit<T>(T data) where T : class
        {
            var source = PipelineMessage == null ? null : PipelineMessage.Sender;
            var message = new PipelineMessage<T>(_pipeline, source, data, PipelineMessage);
            _pipeline.EmitMessage(message);
        }
    }
}
