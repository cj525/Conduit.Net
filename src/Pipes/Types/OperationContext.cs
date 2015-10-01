using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Pipes.Extensions;

namespace Pipes.Types
{
    public class OperationContext : IDisposable
    {
        private readonly List<Action> _onComplete = new List<Action>();

        protected readonly Dictionary<Type, object> Adjuncts = new Dictionary<Type, object>();

        private int _messagesInFlight;
        private int _contextHolds;


        public bool IsCancelled { get; private set; }

        public virtual void Cancel()
        {
            IsCancelled = true;
        }

        public virtual void Close() { }

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

        public void AcquireContextHold()
        {
            Interlocked.Increment(ref _contextHolds);
        }

        public void ReleaseContextHold()
        {
            Interlocked.Decrement(ref _contextHolds);
        }

        public bool IsComplete()
        {
            if (_messagesInFlight < 0)
                throw new DataMisalignedException("Messages In Flight is less than 0.");

            if (_contextHolds < 0)
                throw new DataMisalignedException("Context Holds is less than 0.");

            return _messagesInFlight == 0 && _contextHolds == 0;
        }

        public void OnComplete(Action action)
        {
            _onComplete.Add(action);
        }


        public void Completed()
        {
            _onComplete.Apply(fn => fn());
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
