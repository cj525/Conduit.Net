using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Pipes.Interfaces;

namespace Pipes.Types
{
    /// <summary>
    /// Provides a thread-safe collection which accumulates (<see cref="ICompletable"/>) items to a maximum threshold and provides
    /// a calm, controlled fault handler.  If the threshold is not reached by the end of an operation (specifically when a <see cref="OperationContext"/>
    /// is closed), the fault buffer will trigger a handler which may retry faulty subcontexts (branches of the operation context).
    /// In addition to the threshold event, the completion and cancel handlers can be used to save state.
    /// Because the class is thread-safe, only one subcontext will trigger the threshold fault.  Subsequent failures will be added to the fault list
    /// and therefore consumer code should anticipate overages during any state-saving code which occurs when the buffer is closed.
    /// </summary>
    /// <remarks>
    /// Enumeration of this collection is NOT thread-safe.  The consumer code should prevent Add operations during enumeration.
    /// Similarly, you should not dispose of this object with pending blocked Add calls, or during a Completion triggered removal.
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    [DebuggerTypeProxy(typeof(FaultBuffer<>.DebugProxy))]
    [DebuggerDisplay("{ToDebugString()}")]
    public class FaultBuffer<T> : IEnumerable<T>, IDisposable, ICancellable
    {
        private readonly object _lockObject = new { };
        private readonly SemaphoreSlim _semaphore;
        private readonly CancellationTokenSource _semaphoreCancelSource;
        private readonly CancellationToken _semaphoreCancelToken;
        private readonly int _maxItems;
        private Slot _head;
        private Slot _tail;
        private int _count;
        private ulong _version;
        private bool _isDisposed;

        public FaultBuffer(int maxItems = 0)
        {
            _maxItems = maxItems;
            if (maxItems > 0)
            {
                _semaphore = new SemaphoreSlim(maxItems);
                _semaphoreCancelSource = new CancellationTokenSource();
                _semaphoreCancelToken = _semaphoreCancelSource.Token;
            }
        }

        public int Count
        {
            get { lock (_lockObject) return _count; }
        }

        public bool IsEmpty
        {
            get { lock (_lockObject) return _count == 0; }
        }

        public bool IsFull
        {
            get { lock (_lockObject) return _count == _maxItems && _maxItems > 0; }
        }

        public ICompletable<T> Add(T data, Action<T> onComplete = null, Action<T> onCancel = null)
        {
            if (_maxItems > 0)
            {
                if (_isDisposed)
                    throw new ObjectDisposedException("CompletionBuffer");

                _semaphore.Wait(_semaphoreCancelToken);
            }

            lock (_lockObject)
            {
                _tail = new Slot(this, data, _tail, onComplete, onCancel);

                if (_head == null)
                {
                    _head = _tail;
                }

                // If disposal happened, don't record the addition
                if (!_isDisposed && !_semaphoreCancelSource.IsCancellationRequested)
                    ItemAdded();

                return _tail;
            }
        }

        private void Remove(Slot item, Action slotAdjustment)
        {
            lock (_lockObject)
            {
                // If container is disposed, there is nothing to do
                if (_isDisposed)
                    return;

                // Make slot adjustment
                slotAdjustment();

                // If this was head, next is head
                if (ReferenceEquals(item, _head))
                {
                    _head = item.Next;
                }

                // If this was tail, previous is tail
                if (ReferenceEquals(item, _tail))
                {
                    _tail = item.Previous;
                }

                // New version
                ItemRemoved();

                // Unblock waiters
                if (_maxItems > 0)
                    _semaphore.Release();
            }
        }

        // Called from LOCK!
        private void ItemAdded()
        {
            _count++;
            NewVersion();
        }

        // Called from LOCK!
        private void ItemRemoved()
        {
            _count--;
            NewVersion();
        }

        // Called from LOCK!
        private void NewVersion()
        {
            _version++;
            if (_version == ulong.MaxValue)
                _version = 0;
        }

        public IEnumerator<T> GetEnumerator()
        {
            var startVersion = _version;
            var node = _head;
            while (startVersion == _version && node != null)
            {
                yield return node;
                node = node.Next;
            }

            if (_version != startVersion)
                throw new InvalidOperationException("Collection was modified during enumeration.  Enumeration is not thread-safe.");
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Cancel()
        {
            _semaphoreCancelSource.Cancel();
        }

        public void Dispose()
        {
            _isDisposed = true;
            _semaphore.Dispose();
        }

        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "Paired methods, Next/Previous, would look silly with different access modifier naming.")]
        protected class Slot : ICompletable<T>, ICancellable
        {
            private readonly FaultBuffer<T> _container;
            private readonly Action<T> _completionAction;
            private readonly Action<T> _cancelAction;
            internal Slot Previous;
            internal Slot Next;

            public T Data { get; private set; }

            public bool IsCompleted { get; private set; }

            internal Slot(FaultBuffer<T> container, T data, Slot previous, Action<T> completionAction, Action<T> cancelAction)
            {
                _completionAction = completionAction;
                _cancelAction = cancelAction;
                _container = container;
                Data = data;
                Previous = previous;

                if (previous != null)
                    previous.Next = this;
            }


            public void Completed()
            {
                Remove(_completionAction);
            }

            public void Cancel()
            {
                Remove(_cancelAction);
            }


            public CompletionManifold Branch(Action completedAction = null)
            {
                return new CompletionManifold(() =>
                {
                    Completed();
                    completedAction?.Invoke();
                });
            }

            private void OnCompleted()
            {
                // Link previous to next
                if (Previous != null)
                {
                    Previous.Next = Next;
                }

                // Link next to previous
                if (Next != null)
                {
                    Next.Previous = Previous;
                }

                // Detach self
                Previous = null;
                Next = null;
            }

            private void Remove(Action<T> action)
            {
                if (!IsCompleted)
                {
                    // Fire notification
                    action?.Invoke(this);

                    // Have container make adjustments 
                    // as well as do ours under it's supervision (aka lock)
                    _container.Remove(this, OnCompleted);

                    // Allow idempotence
                    IsCompleted = true;
                }
            }

            // Allow icky casting if that's what they want. 
            // For all intents and purposes, this IS the data
            public static implicit operator T(Slot slot)
            {
                return slot.Data;
            }

            // For all intents and purposes, this IS the data
            public override string ToString()
            {
                return Data.ToString();
            }

            // For all intents and purposes, this IS the data
            public override bool Equals(object obj)
            {
                return Data.Equals(obj);
            }

            // For all intents and purposes, this IS the data
            public override int GetHashCode()
            {
                return Data.GetHashCode();
            }
        }

        private class DebugProxy
        {
            private readonly FaultBuffer<T> _buffer;

            public DebugProxy(FaultBuffer<T> buffer)
            {
                _buffer = buffer;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public object Values => _buffer.ToArray();
        }

        private string ToDebugString()
        {
            lock (_lockObject)
            {
                var result = "Count = " + _count;

                if (_maxItems > 0)
                    result += " of " + _maxItems;

                return result;
            }
        }
    }

}
