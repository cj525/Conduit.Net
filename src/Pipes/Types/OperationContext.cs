using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Pipes.Abstraction;
using Pipes.Extensions;
using Pipes.Interfaces;

namespace Pipes.Types
{
    public class OperationContext : CompletionSource, IOperationContext, IBranchableCompletionSource
    {
        private readonly List<CompletionAction> _onComplete = new List<CompletionAction>();
        private readonly List<CancelAction> _onCancelled = new List<CancelAction>();
        private readonly List<FaultAction> _onFaulted = new List<FaultAction>();
        private readonly Dictionary<Type, ICompletionSource> _completionSources =  new Dictionary<Type, ICompletionSource>();

        protected readonly Dictionary<Type, object> Adjuncts = new Dictionary<Type, object>();

        private int _messagesInFlight;
        private int _contextHolds;
        private CompletionSource _completion;
        

        public OperationContext()
        {
            //AssignActions(OnCompletion, OnCancel, OnFault);
        }

        public void RegisterOnCompletion(CompletionAction completionAction)
        {
            _onComplete.Add(completionAction);
        }

        public void RegisterOnCancellation(CancelAction cancellationAction)
        {
            _onCancelled.Add(cancellationAction);
        }

        public void RegisterOnFault(FaultAction faultAction)
        {
            _onFaulted.Add(faultAction);
        }

        public override void Completed()
        {
            // Complete anything that can be completed
            RetrieveDerived<ICompletable>()?.Apply(adjunct => adjunct.Complete());

            // Trigger any registered callbacks
            _onComplete.Apply(fn => fn());

            // Chain to base
            base.Completed();
        }

        public override void Cancel(string reason)
        {
            // Cancel anything that can be cancelled
            RetrieveDerived<ICancellable>()?.Apply(adjunct => adjunct.Cancel(reason));

            // Trigger any registered callbacks
            _onCancelled.Apply(fn => fn(reason));

            // Chain to base
            base.Cancel(reason);
        }

        public override void Fault(Exception exception)
        {
            // Fault anything that can be faulted
            RetrieveDerived<IFaultable>()?.Apply(adjunct => adjunct.Fault(exception));

            // Trigger any registered callbacks
            _onFaulted.Apply(fn => fn(exception.Message, exception));

            // Chain to base
            base.Fault(exception);
        }

        public override void Fault(string reason, Exception exception = null)
        {
            // Fault anything that can be faulted
            RetrieveDerived<IFaultable>()?.Apply(adjunct => adjunct.Fault(reason, exception));

            // Trigger any registered callbacks
            _onFaulted.Apply(fn => fn(reason, exception));

            // Chain to base
            base.Fault(reason,exception);
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
                if (_messagesInFlight != 0 || _contextHolds != 0 || !base.IsCompleted)
                {
                    return false;
                }

                var adjuncts = RetrieveDerived<ICompletable>().ToArray();
                return adjuncts.All(c => c.IsCompleted);
            }
        }

        /// <summary>
        /// Returns true if a cancel occured in the context or any of the adjuncts
        /// </summary>
        public override bool IsCancelled
        {
            get
            {
                var adjuncts = RetrieveDerived<ICancellable>().ToArray();
                return base.IsCancelled || adjuncts.Any(c => c.IsCancelled);
            }
        }

        /// <summary>
        /// Returns true if a fault occured in the context or any of the adjuncts
        /// </summary>
        public override bool IsFaulted
        {
            get
            {
                var adjuncts = RetrieveDerived<IFaultable>().ToArray();
                return base.IsFaulted || adjuncts.Any(c => c.IsFaulted);
            }
        }


        public CompletionBuffer<T> InitializeCompletionBuffer<T>(int concurrentLimit, CompletionAction completionAction = null, CancelAction cancelAction = null, FaultAction faultAction = null)
        {
            var type = typeof (T);
            if( _completionSources.ContainsKey(type))
                throw new CompletionBufferAlreadyInitializedException(GetType(), type);

            var buffer = new CompletionBuffer<T>(concurrentLimit);
            _completionSources.Add(type, buffer);
            return buffer;
        }

        public ICompletionSource<T> AddCompletionFor<T>(T data, CompletionAction completionAction = null, CancelAction cancelAction = null, FaultAction faultAction = null)
        {
            var type = typeof(T);
            if (!_completionSources.ContainsKey(type))
                throw new CompletionBufferNotInitializedException(GetType(), type);

            var completionSource =_completionSources[type];
            var result = ((CompletionBuffer<T>) completionSource).Add(data, completionAction, cancelAction, faultAction);

            return result;
        }

        protected void InitializeSubcontext(OperationContext subcontext)
        {
            Adjuncts.Apply(kv => subcontext.Adjuncts.Add(kv.Key, kv.Value));
        }



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
            var type = typeof(T);
            if (Adjuncts.ContainsKey(type))
                return (T) Adjuncts[type];
            else
            {
                var result = factory();
                Adjuncts.Add(type, result);
                return result;
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

        public void MessageInFlight()
        {
            Interlocked.Increment(ref _messagesInFlight);
        }

        public void MessageCompleted()
        {
            Interlocked.Decrement(ref _messagesInFlight);
        }

        public DisposableContextHold AcquireDisposableContextHold()
        {
            return new DisposableContextHold(this);
        }


        public async Task WaitForIdle(int waitTimeSliceMs)
        {
            while (!IsCompleted && !IsCancelled && !IsFaulted)
            {
                await Task.Delay(waitTimeSliceMs);
            }
        }


        public void Dispose()
        {
            // Do closing action (like disposals)
            if (!IsCompleted && !IsCancelled && !IsFaulted)
                Close();
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
