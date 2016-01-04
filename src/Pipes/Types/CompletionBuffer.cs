using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Pipes.Abstraction;
using Pipes.Interfaces;

namespace Pipes.Types
{
    /// <summary>
    /// Provides a thread-safe collection which uses a token (<see cref="CompletionSource"/>) to remove items from the collection. 
    /// If a maximum number of items is specified during construction, the collection will block on the Add call until an item is removed.
    /// This class is built-in to the <see cref="OperationContext"/>.  You should not create this class directly unless you understand the 
    /// potential side-effects of having multiple CompletionBuffers in the Adjunct collection.
    /// </summary>
    /// <remarks>
    /// Enumeration of this collection is NOT thread-safe.  The consumer code should prevent Add/Remove during enumeration.
    /// Similarly, you should not dispose of this object with pending blocked Add calls, or during a Completion triggered removal.
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    [DebuggerTypeProxy(typeof(CompletionBuffer<>.DebugProxy))]
    [DebuggerDisplay("{ToDebugString()}")]
    public class CompletionBuffer<T> : CompletionSource, IEnumerable<T>, IDisposable
    {
        private readonly object _lockObject = new {};
        private readonly SemaphoreSlim _semaphore;
        private readonly CancellationTokenSource _semaphoreCancelSource;
        private readonly CancellationToken _semaphoreCancelToken;
        private readonly int _maxItems;
        private Slot _head;
        private Slot _tail;
        private int _count;
        private ulong _version;
        private bool _isDisposed;

        public CompletionBuffer(CompletionAction completionAction = null, CancellationAction cancelAction = null, FaultAction faultAction = null) : base(completionAction, cancelAction, faultAction)
        {
            
        }

        public CompletionBuffer(int maxItems = 0, CompletionAction completionAction = null, CancellationAction cancelAction = null, FaultAction faultAction = null) : base(completionAction,cancelAction,faultAction)
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
            get { lock (_lockObject) return  _count == _maxItems && _maxItems > 0; }
        }

        public ICompletionSource<T> AddWithSingleAction(T data, Action onAnything)
        {
            return Add(data, () => onAnything(), reason => onAnything(), (reason, exception) => onAnything());
        }

        public ICompletionSource<T> Add(T data, CompletionAction onSuccess, FaultAction onFailure)
        {
            return Add(data, onSuccess, (reason) => onFailure(reason), onFailure);
        }

        public ICompletionSource<T> Add(T data, CompletionAction completionAction = null, CancelAction cancelAction = null, FaultAction faultAction = null)
        {
            if (_maxItems > 0 )
            {
                if (_isDisposed)
                    throw new ObjectDisposedException("CompletionBuffer");
                    
                _semaphore.Wait(_semaphoreCancelToken);
            }

            lock (_lockObject)
            {
                _tail = new Slot(this, data, _tail, completionAction, cancelAction, faultAction);

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
        public void Cancel()
        {
            _semaphoreCancelSource.Cancel();
        }

        public IEnumerator<T> GetEnumerator()
        {
            var startVersion = _version;
            var node = _head;
            while (startVersion == _version && node != null)
            {
                yield return node.Data;
                node = node.Next;
            }

            if (_version != startVersion)
                throw new InvalidOperationException("Collection was modified during enumeration.  Enumeration is not thread-safe.");
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispose()
        {
            _isDisposed = true;
            _semaphore.Dispose();
        }

        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "Paired methods, Next/Previous, would look silly with different access modifier naming.")]
        protected class Slot : CompletionSource, ICompletionSource<T>, IBranchableCompletionSource
        {
            private readonly CompletionBuffer<T> _container;
            internal Slot Previous;
            internal Slot Next;

            public T Data { get; private set; }

            internal Slot(CompletionBuffer<T> container, T data, Slot previous, CompletionAction completionAction = null, CancelAction cancelAction = null, FaultAction faultAction = null) : base( completionAction, cancelAction, faultAction)
            {
                _container = container;
                Data = data;
                Previous = previous;

                if (previous != null)
                    previous.Next = this;
            }


            public override void Completed()
            {
                Remove(base.Completed);
            }

            public override void Cancel(string reason)
            {
                Remove(() => base.Cancel(reason));
            }

            public override void Fault(Exception exception)
            {
                Remove(() => base.Fault(exception));
            }

            public override void Fault(string reason, Exception exception = null)
            {
                Remove(() => base.Fault(reason,exception));
            }


            /// <summary>
            /// Branching will create a new manifold that completes a dependant in this manifold 
            /// when the new manifold is completed, cancelled, or faulted.
            /// </summary>
            /// <param name="completionAction"></param>
            /// <param name="cancelAction"></param>
            /// <param name="faultAction"></param>
            /// <returns></returns>
            public CompletionManifold Branch(CompletionAction completionAction = null, CancelAction cancelAction = null, FaultAction faultAction = null)
            {
                // Completion chains
                if (completionAction == null)
                    completionAction = Completed;

                if (cancelAction == null)
                    cancelAction = Cancel;

                if (faultAction == null)
                    faultAction = Fault;
                
                // Return new, chained completion manifold
                return new CompletionManifold(completionAction, cancelAction, faultAction);
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

            private void Remove(Action action)
            {
                if( !IsCompleted )
                {
                    // Fire notification
                    action?.Invoke();

                    // Have container make adjustments 
                    // as well as do ours under it's supervision (aka lock)
                    _container.Remove( this, OnCompleted );

                    // Allow idempotence
                    IsCompleted = true;
                }
            }

            //// Allow icky casting if that's what they want. 
            //// For all intents and purposes, this IS the data
            //public static implicit operator T(Slot slot)
            //{
            //    return slot.Data;
            //}

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
            private readonly CompletionBuffer<T> _buffer;

            public DebugProxy(CompletionBuffer<T> buffer)
            {
                _buffer = buffer;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public object Values
            {
                get { return _buffer.ToArray(); }
            }
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

