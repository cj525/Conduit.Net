using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Pipes.Exceptions;
using Pipes.Extensions;
using Pipes.Implementation;
using Pipes.Interfaces;

namespace Pipes.Abstraction
{
    public abstract class PipelineComponent 
    {
        private readonly List<Action<Pipeline>> _attachActions = new List<Action<Pipeline>>();

        private Pipeline _pipeline;

        protected abstract void Describe(IPipelineComponentBuilder thisComponent);


        public void Build()
        {
            Describe(new Builder(this));
        }


        internal void OnAttach(Action<Pipeline> action)
        {
            _attachActions.Add(action);
        }

        internal void AttachTo(Pipeline pipeline)
        {
            _pipeline = pipeline;
            pipeline.ApplyOver(_attachActions);
        }


        protected void Emit<T>(T data) where T : class
        {
            if (_pipeline == null)
                throw new NotAttachedException("Unattached Component cannot transmit.");

            _pipeline.EmitMessage(new PipelineMessage<T>(_pipeline, this, data));
        }

        protected async Task EmitAsync<T>(T data) where T : class
        {
            if (_pipeline == null)
                throw new NotAttachedException("Unattached Component cannot transmit.");

            await _pipeline.EmitMessageAsync(new PipelineMessage<T>(_pipeline, this, data));
        }

        protected void EmitChain<T>(IPipelineMessage message, T data) where T : class
        {
            message.EmitChain(this, data);
        }

        protected async Task EmitChainAsync<T>(IPipelineMessage message, T data) where T : class
        {
            await message.EmitChainAsync(this, data);
        }

        protected void Terminate()
        {
            _pipeline.Terminate();
        }

        internal void TerminateSource( IPipelineMessage source )
        {
            _pipeline.Terminate(source.Sender);
        }
        
        protected class EmissionRegistrar
        {
            private readonly IPipelineComponentBuilder _builder;

            public EmissionRegistrar(IPipelineComponentBuilder builder)
            {
                _builder = builder;
            }

            public void Emits<T>() where T : class
            {
                _builder.Emits<T>();
            }
        }
    }
}
