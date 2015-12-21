using System;
using Pipes.Abstraction;
using Pipes.Interfaces;

namespace Pipes.Types
{
    public class CompletionManifold : CompletionSource, IBranchableCompletionSource
    {
        private readonly object _lockObject = new { };

        private int _dependencyCount;
        

        public CompletionManifold(CompletionAction completionAction = null, CancelAction cancelAction = null, FaultAction faultAction = null) : base(completionAction, cancelAction, faultAction)
        {
        }


        public int DependencyCount { get { return _dependencyCount; } }

        public override bool IsCompleted { get { lock (_lockObject) return _dependencyCount == 0; } }


        public void RemoveDependent()
        {
            lock (_lockObject)
            {
                if (--_dependencyCount == 0 && (IsCancelled || IsFaulted))
                {
                    // Hookup base implementation because it was intercepted
                    base.Completed();
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

        public override void Completed()
        {
            // Itercept completion
            RemoveDependent();
            // No call to base
        }

        public override string ToString()
        {
            return _dependencyCount + " dependents";
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


            // Return new completion manifold that wraps to this
            return new CompletionManifold(completionAction, cancelAction, faultAction);
        }
    }
}
