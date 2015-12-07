using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Pipes.Extensions;
using Pipes.Interfaces;

namespace Pipes.Types
{
    public class OperationContext : IDisposable, IOperationContext
    {
        private readonly List<Action> _onComplete = new List<Action>();
        private readonly List<Action> _onCancelled = new List<Action>();

        protected readonly Dictionary<Type, object> Adjuncts = new Dictionary<Type, object>();

        private int _messagesInFlight;
        private int _contextHolds;


        public bool IsCancelled { get; private set; }

        public void RegisterOnCancellationAction(Action action)
        {
            _onCancelled.Add(action);
        }

        /// <summary>
        /// Marks the context as cancelled and cancels any <see cref="ICancellable"/> adjuncts
        /// </summary>
        public virtual void Cancel()
        {
            // Cancel anything that can be cancelled
            RetrieveDerived<ICancellable>()?.Apply(adjunct => adjunct.Cancel());

            // Fire any cancellation actions
            _onCancelled.Apply(action => action());

            // Store flag for any code that uses polling methodology
            IsCancelled = true;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void Close() { }



        public bool IsCompleted 
        {
            get
            {
                if (_messagesInFlight < 0)
                    throw new DataMisalignedException("Messages In Flight is less than 0.");

                if (_contextHolds < 0)
                    throw new DataMisalignedException("Context Holds is less than 0.");

                return _messagesInFlight == 0 && _contextHolds == 0;
            }
        }

        public void RegisterOnCompleteAction(Action action)
        {
            _onComplete.Add(action);
        }


        public virtual void Completed()
        {
            _onComplete.Apply(fn => fn());
        }

        public virtual CompletionManifold BranchCompletion()
        {
            return (CompletionManifold)Replace<ICompletable>(completionEntry => completionEntry.Branch());
        }



        public void AcquireContextHold()
        {
            Interlocked.Increment(ref _contextHolds);
        }

        public void ReleaseContextHold()
        {
            Interlocked.Decrement(ref _contextHolds);
        }



        public T Store<T>(T adjunct)
        {
            var type = typeof(T);
            if (Adjuncts.ContainsKey(type))
                throw new DuplicateAdjunctException(GetType(), type);

            Adjuncts.Add(type, adjunct);

            return adjunct;
        }

        public void Store<T>() where T : class, new()
        {
            Store(new T());
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



        public bool Contains<T>()
        {
            return Adjuncts.ContainsKey(typeof(T));
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

        public T[] RetrieveDerived<T>()
        {
            var type = typeof(T);

            return Adjuncts.Where(kv => type.IsAssignableFrom(kv.Key)).Select(kv => (T)kv.Value).ToArray();
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


        public async Task WaitForIdle()
        {
            while (_messagesInFlight > 0 || _contextHolds > 0)
            {
                await Task.Delay(200);
            }
        }


        public void Dispose()
        {
            // Find disposables and dispose
            var isDisposable = (Func<Type, bool>)typeof(IDisposable).IsAssignableFrom;
            Adjuncts.Where(kv => isDisposable(kv.Key)).Apply(kv => ((IDisposable)kv.Value).Dispose());
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

    }


}
