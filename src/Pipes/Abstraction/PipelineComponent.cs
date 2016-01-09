using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Pipes.Exceptions;
using Pipes.Extensions;
using Pipes.Implementation;
using Pipes.Interfaces;
using Pipes.Types;

namespace Pipes.Abstraction
{
    public abstract class PipelineComponent : PipelineComponent<OperationContext> { }

    public abstract class PipelineComponent<TContext> : IPipelineComponent<TContext> where TContext : OperationContext
    {
        private readonly List<Action<Pipeline<TContext>>> _attachActions = new List<Action<Pipeline<TContext>>>();

        private Pipeline<TContext> _pipeline;

        protected static Task NoOp => Target.EmptyTask;

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
        protected void Emit<T>(IPipelineMessage<TContext> message, T data, object meta = null) where T : class
        {
            message.Chain(this, data, meta);
        }

        //[DebuggerHidden]
        //protected Task EmitAsync<T>(IPipelineMessage<TContext> message, T data, object meta = null) where T : class
        //{
        //    return message.ChainAsync(this, data, meta);
        //}

        public virtual void Dispose()
        {
            
        }



        protected async Task Loop<T>( TContext context, LoopState<T> loopState ) where T : class
        {
            while (!context.IsCancelled)
            {
                if (!await loopState.AdvanceAsync())
                    break;

                loopState.Yield();
            }

            await loopState.Complete();
        }
        
        protected abstract class LoopState<T> where T : class 
        {
            // Implements IAsyncEnumerator but it's not officially BCL yet
            // and I am controlling visibility anyway, and since you can't
            // internally implement and interface, we'll forge ahead without

            private long _concurrentYields;

            private readonly IPipelineMessage<TContext> _message;
            private readonly IPipelineComponent<TContext> _component;

            protected LoopState(IPipelineComponent<TContext> component, IPipelineMessage<TContext> message)
            {
                _component = component;
                _message = message;
            }

            protected internal abstract Task<bool> AdvanceAsync();

            protected abstract T Current { get; }

            protected object Meta { get; set; }

            internal virtual void Yield()
            {
                _message.Chain(_component, Current, Meta);
            }

            internal async Task Complete()
            {
                while (Interlocked.Read(ref _concurrentYields) > 0)
                {
                    await Task.Delay(1);
                }
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

            public Observer(PipelineComponent<TContext> component, IPipelineMessage<TContext> message, object aux)
            {
                if (component == null)
                    throw new ArgumentNullException(nameof(component));

                if (message == null)
                    throw new ArgumentNullException(nameof(message));

                _aux = aux;
                _message = message;
                _component = component;
            }

            public void OnCompleted()
            {
                var complete = new ObservationComplete<TData>(_aux);

                if (_emitComplete)
                {
                    //_message.Chain(_component, complete);
                    _component.Emit(_message, complete, new ObserverMeta(typeof(TData)));
                }

                _disposables.Apply(item => item.Dispose());
            }

            public void OnError(Exception exception)
            {
                _disposables.Apply(item => item.Dispose());
                _message.Context.HandleException(exception);
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

            public Observer<TData> DontEmitCompleteMessage()
            {
                _emitComplete = false;

                return this;
            }
        }

        public class ObserverMeta
        {
            public Type OfType { get; }

            public ObserverMeta(Type type)
            {
                OfType = type;
            }

        }
    }
}
