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

        protected IObserver<T> ObserveFor<T>() where T : class
        {
            return new Observer<T>(this, null, null);
        }

        protected IObserver<T> ObserveFor<T>(object token) where T : class
        {
            return new Observer<T>(this, null, token);
        }

        protected IObserver<T> ObserveFor<T>(IPipelineMessage message) where T : class
        {
            return new Observer<T>(this, message, null);
        }

        protected IObserver<T> ObserveFor<T>(IPipelineMessage message, object token) where T : class
        {
            return new Observer<T>(this, message, token);
        }

        public class ObservationComplete<T>
        {
            public Type Type { get; private set; }
            public object Token { get; private set; }

            public ObservationComplete(object token)
            {
                Type = typeof (T);
                Token = token;
            }
        }
        private class Observer<T> : IObserver<T> where T : class
        {
            private readonly PipelineComponent _component;
            private readonly IPipelineMessage _message;
            private readonly object _token;

            public Observer(PipelineComponent component, IPipelineMessage message, object token)
            {
                _token = token;
                _message = message;
                _component = component;
            }

            public void OnCompleted()
            {
                var complete = new ObservationComplete<T>(_token);

                if (_message != null)
                    _component.EmitChain(_message, complete);
                else
                    _component.Emit(complete);
            }

            public void OnError(Exception error)
            {
                if (_message != null)
                    _component.EmitChain(_message, error);
                else
                    _component.Emit(error);
            }

            public void OnNext(T value)
            {
                if( _message != null )
                    _component.EmitChain( _message, value );
                else
                    _component.Emit(value);
            }


        }
    }
}
