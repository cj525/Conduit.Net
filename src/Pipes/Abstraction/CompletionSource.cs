using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Pipes.Interfaces;
using Pipes.Types;

namespace Pipes.Abstraction
{
    /// <summary>
    /// sdf
    /// </summary>
    /// <remarks>
    /// The default implementation of Branch(...) is to complete when all children are completed or all but one is cancelled, cancel when all children are cancelled, and fault when one child faults (cancelling the rest).
    /// </remarks>
    public abstract class CompletionSource : ICompletionSource, ICompletionToken, ICompletionEventRegistrar
    {
        private readonly List<CompletionTask> _completionTasks = new List<CompletionTask>();
        private readonly List<CancellationTask> _cancelTasks = new List<CancellationTask>();
        private readonly List<FaultTask> _faultTasks = new List<FaultTask>();

        public virtual bool IsCompleted { get; protected set; }

        public virtual bool IsCancelled { get; protected set; }

        public virtual bool IsFaulted { get; protected set; }

        public virtual bool IsUnset { get { return !IsCompleted && !IsCancelled && !IsFaulted; } }

        public virtual async Task Complete()
        {
            IsCompleted = true;
            if (_completionTasks.Any())
                await Task.WhenAll(_completionTasks.Select(task=>task()));
            else
                await Target.EmptyTask;
        }

        public virtual async Task Cancel(string reason = null)
        {
            IsCancelled = true;
            if (_cancelTasks.Any())
                await Task.WhenAll(_cancelTasks.Select(task => task(reason)));
            else
                await Target.EmptyTask;
        }

        public virtual async Task Fault(Exception exception)
        {
            IsFaulted = true;
            if (_faultTasks.Any())
                await Task.WhenAll(_faultTasks.Select(task => task("Completable task faulted", exception)));
            else
                await Target.EmptyTask;
        }

        public virtual async Task Fault(string reason, Exception exception = null)
        {
            IsFaulted = true;
            if (_faultTasks.Any())
                await Task.WhenAll(_faultTasks.Select(task => task(reason,exception)));
            else
                await Target.EmptyTask;
        }

        public async Task WaitForCompletion(int? timeoutMs = null, int waitTimeSliceMs = 200)
        {
            var timeoutStamp = timeoutMs.HasValue ? DateTime.UtcNow.AddMilliseconds(timeoutMs.Value) : DateTime.MaxValue;
            while (IsUnset && DateTime.UtcNow < timeoutStamp)
            {
                await Task.Delay(waitTimeSliceMs);
            }
        }

        public void AddCancallationTask(CancellationTask task)
        {
            _cancelTasks.Add(task);
        }

        public void AddCompletionTask(CompletionTask task)
        {
            _completionTasks.Add(task);
        }

        public void AddFaultTask(FaultTask task)
        {
            _faultTasks.Add(task);
        }
    }
}
