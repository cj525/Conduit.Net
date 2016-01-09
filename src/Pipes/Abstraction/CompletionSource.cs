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
    /// The default implementation of Branch(...) will:
    ///     Complete when all children are completed or at least one child is completed and the rest are cancelled.
    ///     Cancel when all children are cancelled.
    ///     Fault when one child faults (cancelling the rest).
    /// </remarks>
    public abstract class CompletionSource : ICompletionSource, ICompletionToken, ICompletionEventRegistrar
    {
        /// <summary>
        /// The amount of time in milliseconds to wait for all messages to complete after a pipeline invocation has completed.
        /// Because all components run as concurrently as possible, it is very likely that invocation will return almost immediately.
        /// If the timeout is reached, the context will be canceled and all messages will need to be accounted for before returning.
        /// The default is (null) which indicates there is no timeout.
        /// </summary>
        public int? WaitForIdleTimeout { get; set; }

        /// <summary>
        /// The amount of time in milliseconds to yield the invocation thread context (as defined by the TPL scheduler) before checking 
        /// for an idle context indicating the invocation and all related messages have completed (successfully or not).  Setting this
        /// value too low can waste cpu cycles, and setting the value too high will inflate latency.  
        /// The default is 25ms.
        /// </summary>
        public int WaitForIdleTimeslice { get; set; } = 25;

        public virtual bool IsCompleted { get; protected set; }

        public virtual bool IsCancelled { get; protected set; }

        public virtual bool IsFaulted { get; protected set; }

        public virtual bool CompletionStateIsUnset => !IsCompleted && !IsCancelled && !IsFaulted;

        /// <summary>
        /// The cancellation token is provided for compatibility with the TPL.
        /// </summary>
        public CancellationToken CancellationToken => _cts.Token;

        private readonly List<CompletionTask> _completionTasks = new List<CompletionTask>();
        private readonly List<CancellationTask> _cancelTasks = new List<CancellationTask>();
        private readonly List<FaultTask> _faultTasks = new List<FaultTask>();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();



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
            _cts.Cancel();
            IsCancelled = true;
            if (_cancelTasks.Any())
                await Task.WhenAll(_cancelTasks.Select(task => task(reason)));
            else
                await Target.EmptyTask;
        }

        public virtual async Task Fault(Exception exception)
        {
            _cts.Cancel();
            IsFaulted = true;
            if (_faultTasks.Any())
                await Task.WhenAll(_faultTasks.Select(task => task("Completable task faulted", exception)));
            else
                await Target.EmptyTask;
        }

        public virtual async Task Fault(string reason, Exception exception = null)
        {
            _cts.Cancel();
            IsFaulted = true;
            if (_faultTasks.Any())
                await Task.WhenAll(_faultTasks.Select(task => task(reason,exception)));
            else
                await Target.EmptyTask;
        }

        public async Task WaitForCompletion()
        {
            var timeoutStamp = WaitForIdleTimeout.HasValue ? DateTime.UtcNow.AddMilliseconds(WaitForIdleTimeout.Value) : DateTime.MaxValue;
            while (CompletionStateIsUnset && DateTime.UtcNow < timeoutStamp)
            {
                await Task.Delay(WaitForIdleTimeslice);
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

        public virtual void Dispose()
        {
            if (CompletionStateIsUnset)
                // TODO: Make Exception for this
                Fault("Did not complete before disposal").Wait();
        }
    }
}
