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
    //public abstract class PipelineComponent : PipelineComponent<object> { }

    public abstract class PipelineComponent<TContext> : IPipelineComponent<TContext> where TContext : class
    {
        private readonly List<Action<Pipeline<TContext>>> _attachActions = new List<Action<Pipeline<TContext>>>();

        private Pipeline<TContext> _pipeline;

        protected abstract void Describe(IPipelineComponentBuilder<TContext> thisComponent);

        void IPipelineComponent<TContext>.Build()
        {
            Describe(new Builder<TContext>(this));
        }

        void IPipelineComponent<TContext>.OnAttach(Action<Pipeline<TContext>> action)
        {
            _attachActions.Add(action);
        }

        void IPipelineComponent<TContext>.AttachTo(Pipeline<TContext> pipeline)
        {
            _pipeline = pipeline;
            pipeline.ApplyOver(_attachActions);
        }

        [DebuggerHidden]
        protected void Emit<TData>(TData data, TContext context = default(TContext)) where TData : class
        {
            if (_pipeline == null)
                throw new NotAttachedException("Unattached Component cannot transmit.");

            _pipeline.EmitMessage(new PipelineMessage<TData,TContext>(_pipeline, this, data, context));
        }

        [DebuggerHidden]
        protected Task EmitAsync<TData>(TData data, TContext context = default(TContext)) where TData : class
        {
            if (_pipeline == null)
                throw new NotAttachedException("Unattached Component cannot transmit.");

            return _pipeline.EmitMessageAsync(new PipelineMessage<TData,TContext>(_pipeline, this, data, context));
        }

        [DebuggerHidden]
        protected void EmitChain<T>(IPipelineMessage<TContext> message, T data, TContext context = default(TContext)) where T : class
        {
            if (context == default(TContext))
                context = message.Context;

            message.EmitChain(this, data, context);
        }

        [DebuggerHidden]
        protected Task EmitChainAsync<T>(IPipelineMessage<TContext> message, T data, TContext context = default(TContext)) where T : class
        {
            if (context == default(TContext))
                context = message.Context;

            return message.EmitChainAsync(this, data, context);
        }

        protected void Terminate()
        {
            _pipeline.Terminate();
        }

        
        protected class EmissionRegistrar
        {
            private readonly IPipelineComponentBuilder<TContext> _builder;

            public EmissionRegistrar(IPipelineComponentBuilder<TContext> builder)
            {
                _builder = builder;
            }

            public EmissionRegistrar Emits<T>() where T : class
            {
                _builder.Emits<T>();
                return this;
            }
        }

        protected Observer<TData> ObserveFor<TData>() where TData : class
        {
            return new Observer<TData>(this, null, null);
        }

        protected Observer<TData> ObserveFor<TData>(object aux) where TData : class
        {
            return new Observer<TData>(this, null, aux);
        }

        protected Observer<TData> ObserveFor<TData>(IPipelineMessage<TContext> message) where TData : class
        {
            return new Observer<TData>(this, message, null);
        }

        protected Observer<TData> ObserveFor<TData>(IPipelineMessage<TContext> message, object aux) where TData : class
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

        public class Observer<TData> : IObserver<TData>
            where TData : class
        {
            private readonly PipelineComponent<TContext> _component;
            private readonly IPipelineMessage<TContext> _message;
            private readonly object _aux;
            private readonly List<IDisposable> _disposables = new List<IDisposable>();

            private bool _emitComplete = true;
            private bool _emitNext = true;
            private bool _emitError = true;

            public Observer(PipelineComponent<TContext> component, IPipelineMessage<TContext> message, object aux)
            {
                _aux = aux;
                _message = message;
                _component = component;
            }

            public void OnCompleted()
            {
                var complete = new ObservationComplete<TData>(_aux);

                if (_emitComplete)
                {
                    if (_message != null)
                        _component.EmitChain(_message, complete);
                    else
                        _component.Emit(complete);
                }

                _disposables.Apply(item => item.Dispose());
            }

            public void OnError(Exception exception)
            {
                if (!_emitError || _message != null)
                {
                    if (!_message.HandleException(exception))
                        throw new OperationCanceledException("Unhandled observer exception",exception);
                }
                else
                    _component.Emit(exception);

                _disposables.Apply(item => item.Dispose());
            }

            public void OnNext(TData value)
            {
                if (_message != null)
                    _component.EmitChain(_message, value);
                else
                    _component.Emit(value);
            }

            public Observer<TData> WithDisposal(IDisposable disposable)
            {
                _disposables.Add(disposable);
                return this;
            }

            public Observer<TData> DontEmitControlMessages()
            {
                _emitComplete = false;
                _emitError = false;

                return this;
            }

            public Observer<TData> ConfigureEmits(bool onError = false, bool onComplete = false)
            {
                _emitComplete = onComplete;
                _emitError = onError;

                return this;
            }
        }
    }
}
