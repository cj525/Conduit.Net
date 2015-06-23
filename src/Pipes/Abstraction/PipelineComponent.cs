using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Pipes.Exceptions;
using Pipes.Extensions;
using Pipes.Implementation;
using Pipes.Interfaces;
using Pipes.Types;

namespace Pipes.Abstraction
{
    public abstract class PipelineComponent : PipelineComponent<object> { }

    public abstract class PipelineComponent<TScope> : IPipelineComponent<TScope>
    {
        private readonly List<Action<Pipeline<TScope>>> _attachActions = new List<Action<Pipeline<TScope>>>();

        private Pipeline<TScope> _pipeline;

        protected abstract void Describe(IPipelineComponentBuilder<TScope> thisComponent);

        void IPipelineComponent<TScope>.Build()
        {
            Describe(new Builder<TScope>(this));
        }

        void IPipelineComponent<TScope>.OnAttach(Action<Pipeline<TScope>> action)
        {
            _attachActions.Add(action);
        }

        void IPipelineComponent<TScope>.AttachTo(Pipeline<TScope> pipeline)
        {
            _pipeline = pipeline;
            pipeline.ApplyOver(_attachActions);
        }
        void IPipelineComponent<TScope>.TerminateSource(IPipelineMessage<TScope> source)
        {
            _pipeline.Terminate(source.Sender);
        }

        [DebuggerHidden]
        protected void Emit<TData>(TData data, TScope scope = default(TScope)) where TData : class
        {
            if (_pipeline == null)
                throw new NotAttachedException("Unattached Component cannot transmit.");

            _pipeline.EmitMessage(new PipelineMessage<TData,TScope>(_pipeline, this, data, scope));
        }

        [DebuggerHidden]
        protected Task EmitAsync<TData>(TData data, TScope scope = default(TScope)) where TData : class
        {
            if (_pipeline == null)
                throw new NotAttachedException("Unattached Component cannot transmit.");

            return _pipeline.EmitMessageAsync(new PipelineMessage<TData,TScope>(_pipeline, this, data, scope));
        }

        [DebuggerHidden]
        protected void EmitChain<T>(IPipelineMessage<TScope> message, T data, TScope scope = default(TScope)) where T : class
        
        {
            message.EmitChain(this, data, scope);
        }

        [DebuggerHidden]
        protected Task EmitChainAsync<T>(IPipelineMessage<TScope> message, T data, TScope scope = default(TScope)) where T : class
        {
            return message.EmitChainAsync(this, data, scope);
        }

        protected void Terminate()
        {
            _pipeline.Terminate();
        }

        
        protected class EmissionRegistrar
        {
            private readonly IPipelineComponentBuilder<TScope> _builder;

            public EmissionRegistrar(IPipelineComponentBuilder<TScope> builder)
            {
                _builder = builder;
            }

            public EmissionRegistrar Emits<T>() where T : class
            {
                _builder.Emits<T>();
                return this;
            }
        }

        protected IObserver<TData> ObserveFor<TData>() where TData : class
        {
            return new Observer<TData>(this, null, null);
        }

        protected IObserver<TData> ObserveFor<TData>(object aux) where TData : class
        {
            return new Observer<TData>(this, null, aux);
        }

        protected IObserver<TData> ObserveFor<TData>(IPipelineMessage<TScope> message) where TData : class
        {
            return new Observer<TData>(this, message, null);
        }

        protected IObserver<TData> ObserveFor<TData>(IPipelineMessage<TScope> message, object aux) where TData : class
        {
            return new Observer<TData>(this, message, aux);
        }

        public class ObservationComplete<T>
        {
            public Type Type { get; private set; }

            public object Aux { get; private set; }

            public ObservationComplete(object aux)
            {
                Type = typeof (T);
                Aux = aux;
            }
        }
        private class Observer<TData> : IObserver<TData> where TData : class
        {
            private readonly PipelineComponent<TScope> _component;
            private readonly IPipelineMessage<TScope> _message;
            private readonly object _aux;

            public Observer(PipelineComponent<TScope> component, IPipelineMessage<TScope> message, object aux)
            {
                _aux = aux;
                _message = message;
                _component = component;
            }

            public void OnCompleted()
            {
                var complete = new ObservationComplete<TData>(_aux);

                if (_message != null)
                    _component.EmitChain(_message, complete);
                else
                    _component.Emit(complete);
            }

            public void OnError(Exception error)
            {
                if (_message != null)
                {
                    _message.RaiseException(error);

                }
                else
                    _component.Emit(error);
            }

            public void OnNext(TData value)
            {
                if( _message != null )
                    _component.EmitChain( _message, value );
                else
                    _component.Emit(value);
            }
        }
    }
}
