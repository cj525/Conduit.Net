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
    public abstract class PipelineComponent : PipelineComponent<IOperationContext> { }

    public abstract class PipelineComponent<TContext> : IPipelineComponent<TContext> where TContext : class, IOperationContext
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
        protected void Emit<T>(IPipelineMessage<TContext> message, T data, TContext subcontext = default(TContext)) where T : class
        {
            message.Chain(this, data, subcontext);
        }

        [DebuggerHidden]
        protected Task EmitAsync<T>(IPipelineMessage<TContext> message, T data, TContext subcontext = default(TContext)) where T : class
        {
            return message.ChainAsync(this, data, subcontext);
        }

        protected void Terminate()
        {
            _pipeline.Terminate();
        }

        protected async Task Loop<T>( TContext context, LoopState<T> loopState ) where T : class
        {
            while (!context.IsCancelled)
            {
                if (!await loopState.AdvanceAsync())
                    break;

                await loopState.Yield();
            }
        }
        
        protected abstract class LoopState<T> where T : class 
        {
            // Implements IAsyncEnumerator but it's not officially BCL yet
            // and I am controlling visibility anyway, and since you can't
            // internally implement and interface, we'll forge ahead without

            private readonly IPipelineMessage<TContext> _message;
            private readonly IPipelineComponent<TContext> _component;

            protected LoopState(IPipelineComponent<TContext> component, IPipelineMessage<TContext> message)
            {
                _component = component;
                _message = message;
            }

            protected internal abstract Task<bool> AdvanceAsync();

            protected abstract T Current { get; }

            internal virtual async Task Yield()
            {
                try
                {
                    await _message.ChainAsync(_component, Current);
                }
                catch (Exception exception)
                {
                    // Give pipeline a chance to handle this
                    if (!_message.HandleException(exception))
                    {
                        // Cancel the context (which will cancel the loop)
                        _message.Context.Fault(exception);
                    }
                }
            }
        }
        protected abstract class ConcurrentLoopState<T> : LoopState<T> where T:class
        {
            protected ConcurrentLoopState(IPipelineComponent<TContext> component, IPipelineMessage<TContext> message) : base(component, message)
            {
            }

            /// <summary>
            /// Because the base class already catches exceptions and faults the current context, this bit of craziness
            /// works to trigger as many concurrent yield actions as possible, but only as fast as the loop state can keep
            /// up, which makes it ideal for parsers where the current line's parsing may or may not depend on the previous line.
            /// </summary>
            internal new async void Yield()
            {
                await base.Yield();
            }
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

        //protected Observer<TData> ObserveFor<TData>() where TData : class
        //{
        //    return new Observer<TData>(this, null, null);
        //}

        //protected Observer<TData> ObserveFor<TData>(object aux) where TData : class
        //{
        //    return new Observer<TData>(this, null, aux);
        //}

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
            private bool _emitError = true;

            public Observer(PipelineComponent<TContext> component, IPipelineMessage<TContext> message, object aux)
            {
                if (component == null)
                    throw new ArgumentNullException("component");

                if (message == null)
                    throw new ArgumentNullException("message");

                _aux = aux;
                _message = message;
                _component = component;
            }

            public void OnCompleted()
            {
                var complete = new ObservationComplete<TData>(_aux);

                if (_emitComplete)
                {
                    _component.Emit(_message, complete);
                }

                _disposables.Apply(item => item.Dispose());
            }

            public void OnError(Exception exception)
            {
                if (!_emitError)
                {
                    if (!_message.HandleException(exception))
                    {
                        _disposables.Apply(item => item.Dispose());
                        throw new OperationCanceledException("Unhandled observer exception", exception);
                    }
                }
                else
                    _component.Emit(_message, exception);

                _disposables.Apply(item => item.Dispose());
            }

            public void OnNext(TData value)
            {
                _component.Emit(_message, value);
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
