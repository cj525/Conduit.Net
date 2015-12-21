using System;
using Pipes.Interfaces;
using Pipes.Types;

namespace Pipes.Abstraction
{
    /// <summary>
    /// This class is built-in to the <see cref="OperationContext"/>.
    /// </summary>
    public abstract class CompletionSource : ICompletionSource
    {
        private CompletionAction _completionAction;
        private CancelAction _cancelAction;
        private FaultAction _faultAction;

        internal void AssignActions(CompletionAction completionAction = null, CancelAction cancelAction = null, FaultAction faultAction = null)
        {
            _completionAction = completionAction;
            _cancelAction = cancelAction;
            _faultAction = faultAction;
        }

        public virtual bool IsCompleted { get; protected set; }

        public virtual bool IsCancelled { get; protected set; }

        public virtual bool IsFaulted { get; protected set; }



        public virtual void Completed()
        {
            IsCompleted = true;
            _completionAction?.Invoke();
        }

        public virtual void Cancel(string reason)
        {
            IsCancelled = true;
            _cancelAction?.Invoke(reason);
        }

        public virtual void Fault(Exception exception)
        {
            IsFaulted = true;
            _faultAction?.Invoke("Exception encountered", exception);
        }

        public virtual void Fault(string reason, Exception exception = null)
        {
            IsFaulted = true;
            _faultAction?.Invoke("Exception encountered", exception);
        }
    }
}
