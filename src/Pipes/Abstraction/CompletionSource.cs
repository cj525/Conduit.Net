using System;
using System.Threading.Tasks;
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
        private CancellationAction _cancelAction;
        private FaultAction _faultAction;

        internal void AssignActions(CompletionAction completionAction = null, CancellationAction cancelAction = null, FaultAction faultAction = null)
        {
            _completionAction = completionAction;
            _cancelAction = cancelAction;
            _faultAction = faultAction;
        }

        public virtual bool IsCompleted { get; protected set; }

        public virtual bool IsCancelled { get; protected set; }

        public virtual bool IsFaulted { get; protected set; }



        public virtual async Task Completed()
        {
            IsCompleted = true;
            if (_completionAction != null)
                await _completionAction();
            else
                await Target.EmptyTask;
        }

        public virtual async Task Cancel(string reason)
        {
            IsCancelled = true;
            if (_cancelAction != null)
                await _cancelAction(reason);
            else
                await Target.EmptyTask;
        }

        public virtual async Task Fault(Exception exception)
        {
            IsFaulted = true;
            if (_faultAction != null)
                await _faultAction("An exception was thrown", exception);
            else
                await Target.EmptyTask;
        }

        public virtual async Task Fault(string reason, Exception exception = null)
        {
            IsFaulted = true;
            if (_faultAction != null)
                await _faultAction(reason, exception);
            else
                await Target.EmptyTask;
        }
    }
}
