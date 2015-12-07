using System;
using Pipes.Interfaces;

namespace Pipes.Types
{
    public class CompletionManifold : ICompletable
    {
        private readonly object _lockObject = new { };
        private int _dependencyCount;
        private readonly Action _action;

        public int DependencyCount { get { return _dependencyCount; } }


        public CompletionManifold(Action action)
        {
            _action = action;
        }

        public void RemoveDependent()
        {
            lock (_lockObject)
            {
                if (--_dependencyCount == 0)
                {
                    _action();
                }

                if (_dependencyCount < 0)
                {
                    throw new IndexOutOfRangeException("CompletionManifold has over-completed!");
                }
            }
        }

        public void AddDependent()
        {
            lock (_lockObject)
            {
                _dependencyCount++;
            }
        }

        public override string ToString()
        {
            return _dependencyCount + " dependents";
        }

        public void Completed()
        {
            RemoveDependent();
        }

        public bool IsCompleted
        {
            get
            {
                lock (_lockObject) return _dependencyCount == 0;
            }
        }

        public CompletionManifold Branch(Action completionAction = null)
        {
            return new CompletionManifold(() =>
            {
                RemoveDependent();
                completionAction?.Invoke();
            });
        }
    }
}
