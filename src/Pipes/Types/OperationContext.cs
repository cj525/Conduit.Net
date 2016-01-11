using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Pipes.Abstraction;
using Pipes.Extensions;
using Pipes.Implementation;
using Pipes.Interfaces;

namespace Pipes.Types
{
    public class OperationContext : CompletionSource, IDisposable
    {
        private readonly List<CompletionTask> _onComplete = new List<CompletionTask>();
        private readonly List<CancellationTask> _onCancelled = new List<CancellationTask>();
        private readonly List<FaultTask> _onFaulted = new List<FaultTask>();
        private readonly Dictionary<Type, ICompletionSource> _completionSources =  new Dictionary<Type, ICompletionSource>();

        protected readonly Dictionary<Type, object> Adjuncts = new Dictionary<Type, object>();
        internal Dictionary<object, Thunk[]> Thunks = new Dictionary<object, Thunk[]>();

        private int _messagesInFlight;
        private int _contextHolds;
        public object[] Components { get; internal set; }
        public OperationContext(  )
        {
            //AssignActions(OnCompletion, OnCancel, OnFault);
        }

        internal void SetComponents(IEnumerable<object> components)
        {
            Components = components.ToArray();
        }

        public void OnCompletion(CompletionTask completionTask)
        {
            _onComplete.Add(completionTask);
        }

        public void OnCancellation(CancellationTask cancellationAction)
        {
            _onCancelled.Add(cancellationAction);
        }

        public void OnFault(FaultTask faultTask)
        {
            _onFaulted.Add(faultTask);
        }


        public virtual void HandleException(Exception exception)
        {
            if (exception is OperationCanceledException)
            {
                Cancel(exception.Message).Wait();
            }
            else
            {
                UnhandledException = exception;
                HasUnhandledException = true;
                Fault(exception).Wait();
            }
        }

        public bool HasUnhandledException { get; protected set; }

        public Exception UnhandledException { get; protected set; }


        public override async Task Complete()
        {
            // Complete anything that can be completed
            RetrieveDerived<ICompletable>()?.Apply(adjunct => adjunct.Complete());

            // Trigger any registered callbacks
            _onComplete.Reverse();
            _onComplete.ApplyAndWait(fn => fn());

            // Chain to base
            await base.Complete();
        }

        public override async Task Cancel(string reason = null)
        {
            // Chain to base
            await base.Cancel(reason);
            await WaitForCompletion();

            // Trigger any registered callbacks
            _onCancelled.Reverse();
            _onCancelled.ApplyAndWait(fn => fn(reason));

        }

        public override async Task Fault(Exception exception)
        {
            // Chain to base
            await base.Fault(exception);
            await WaitForCompletion();

            // Trigger any registered callbacks
            _onFaulted.Reverse();
            _onFaulted.ApplyAndWait(fn => fn(exception.Message, exception));
        }

        public override async Task Fault(string reason, Exception exception = null)
        {
            // Chain to base
            await base.Fault(reason, exception);
            await WaitForCompletion();

            // Trigger any registered callbacks
            _onFaulted.ApplyAndWait(fn => fn(reason, exception));
        }

        /// <summary>
        /// Close is called by completion, cancellation, or a fault of this context
        /// </summary>
        public virtual void Close()
        {
            ApplyOptionalAdjunct<IDisposable>(instance => instance.Dispose());
        }

        /// <summary>
        /// Returns true if a there are no messages in flight and there are no context holds and all adjuncts are marked as complete
        /// </summary>
        public override bool IsCompleted 
        {
            get
            {
                if (_messagesInFlight < 0)
                    throw new DataMisalignedException("Messages In Flight is less than 0.");

                if (_contextHolds < 0)
                    throw new DataMisalignedException("Context Holds is less than 0.");

                // Take the easy way out
                if (_messagesInFlight != 0 || _contextHolds != 0)
                {
                    return false;
                }

                return true;
            }
        }



        internal CompletionBuffer<T> InitializeCompletionBuffer<T>(int concurrentLimit, CompletionTask completionTask = null, CancellationTask cancellationTask = null, FaultTask faultTask = null)
        {
            var type = typeof (T);
            if( _completionSources.ContainsKey(type))
                throw new CompletionBufferAlreadyInitializedException(GetType(), type);

            var buffer = new CompletionBuffer<T>(concurrentLimit);
            _completionSources.Add(type, buffer);
            return buffer;
        }

        internal ICompletionSource AddCompletionFor<T>(T data, CompletionTask completionTask = null, CancellationTask cancellationTask = null, FaultTask faultTask = null)
        {
            var type = typeof(T);
            if (!_completionSources.ContainsKey(type))
                throw new CompletionBufferNotInitializedException(GetType(), type);

            var completionSource =_completionSources[type];
            var result = ((CompletionBuffer<T>) completionSource).AddItem(data, completionTask, cancellationTask, faultTask);

            return result;
        }

        //protected void InitializeSubcontext(OperationContext subcontext)
        //{
        //    Adjuncts.Apply(kv => subcontext.Adjuncts.Add(kv.Key, kv.Value));
        //}



        /// <summary>
        /// Creates a completor
        /// </summary>
        /// <param name="onComplete"></param>
        /// <returns></returns>
        public Completor CreateCompletor(Action<OperationContext> onComplete)
        {
            // Default to Close the context
            if (onComplete == null)
                onComplete = c => c.Close();

            return new Completor(this, onComplete);
        }




        public void AcquireContextHold()
        {
            Interlocked.Increment(ref _contextHolds);
        }

        public void ReleaseContextHold()
        {
            Interlocked.Decrement(ref _contextHolds);
        }



        public bool ContainsAdjunctOfType<T>()
        {
            return Adjuncts.ContainsKey(typeof(T));
        }

        public bool ContainsAdjunctAssignableTo<T>()
        {
            var type = typeof(T);
            return Adjuncts.Any(kv => type.IsAssignableFrom(kv.Key));
        }

        public T Ensure<T>(Func<T> factory)
        {
            lock (Adjuncts)
            {
                var type = typeof (T);
                if (Adjuncts.ContainsKey(type))
                    return (T) Adjuncts[type];
                else
                {
                    var result = factory();
                    Adjuncts.Add(type, result);
                    return result;
                }
            }
        }

        public T Store<T>(T adjunct)
        {
            var type = typeof(T);
            if (Adjuncts.ContainsKey(type))
                throw new DuplicateAdjunctException(GetType(), type);

            Adjuncts.Add(type, adjunct);

            return adjunct;
        }

        public T Store<T>() where T : class, new()
        {
            return Store(new T());
        }

        public T Remove<T>()
        {
            var type = typeof(T);
            var adjunct = Retrieve<T>();
            Adjuncts.Remove(type);
            return adjunct;
        }

        public T Replace<T>(Func<T, T> func)
        {
            return Store(func(Remove<T>()));
        }

        public T Retrieve<T>()
        {
            var type = typeof(T);
            if (!Adjuncts.ContainsKey(type))
            {
                return default(T);
            }
            return (T)Adjuncts[type];
        }

        public IEnumerable<T> RetrieveDerived<T>()
        {
            var type = typeof(T);

            return Adjuncts.Where(kv => type.IsAssignableFrom(kv.Key)).Select(kv => (T)kv.Value);
        }

        public void ApplyOptionalAdjunct<T>(Action<T> operation)
        {
            RetrieveDerived<T>().Apply(operation);
        }

        public virtual void MessageInFlight()
        {
            Interlocked.Increment(ref _messagesInFlight);
        }

        public virtual void MessageCompleted()
        {
            Interlocked.Decrement(ref _messagesInFlight);
        }

        public DisposableContextHold AcquireDisposableContextHold()
        {
            return new DisposableContextHold(this);
        }

        public override void Dispose()
        {
            // Do closing action (like disposals)
            if (!IsCompleted && !IsCancelled && !IsFaulted)
                Close();

            // Dispose components
            Components.Apply(component => ((IPipelineComponent)component).Dispose());

            base.Dispose();
        }


        public class DisposableContextHold : IDisposable
        {
            private readonly OperationContext _context;

            public DisposableContextHold(OperationContext context)
            {
                _context = context;
                context.AcquireContextHold();
            }

            public void Dispose()
            {
                _context.ReleaseContextHold();
            }
        }


        public class Completor
        {
            private readonly Action<OperationContext> _contextAction;
            private readonly OperationContext _context;

            public Completor(OperationContext context, Action<OperationContext> contextAction)
            {
                _context = context;
                _contextAction = contextAction;
            }

            public void Trigger()
            {
                _contextAction(_context);
            }

            public Action AsTrigger()
            {
                return Trigger;
            }

            public Action<T> AsAction<T>()
            {
                return _ => Trigger();
            }
        }

        public class DuplicateAdjunctException : Exception
        {
            public DuplicateAdjunctException(Type source, Type adjunct)
                : base(String.Concat("Context ", source.Name, " already register adjunct of type ", adjunct.Name))
            {
            }
        }
        public class NoSuchAdjunctException : Exception
        {
            public NoSuchAdjunctException(Type source, Type adjunct)
                : base(String.Concat("Context ", source.Name, " does not contain adjunct of type ", adjunct.Name))
            {
            }
        }

        public class CompletionBufferNotInitializedException : Exception
        {
            public CompletionBufferNotInitializedException(Type source, Type attemptedToAddType)
                : base(String.Concat("Context ", source.Name, " does not have a completion buffer of type ", attemptedToAddType.Name))
            {
            }
        }
        public class CompletionBufferAlreadyInitializedException : Exception
        {
            public CompletionBufferAlreadyInitializedException(Type source, Type attemptedToAddType)
                : base(String.Concat("Context ", source.Name, " already has a completion buffer of type ", attemptedToAddType.Name))
            {
            }
        }

    }


}
