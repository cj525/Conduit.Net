using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Pipes.Abstraction;

namespace Pipes.Interfaces
{

    public delegate Task CompletionTask();

    public delegate Task CancellationTask(string reason = null);

    public delegate Task FaultTask(string reason = null, Exception exception = null);

    public delegate Task BranchedCompletionTask(ICompletionSource source);
    public delegate Task BranchedCancellationTask(ICompletionSource source, string reason = null);
    public delegate Task BranchedFaultTask(ICompletionSource source, string reason = null, Exception exception = null);



    /// <summary>
    /// If you store an object which implements this interface inside the context, the cancel call will be chained
    /// </summary>
    public interface ICancellable
    {
        Task Cancel(string reason = null);
    }

    /// <summary>
    /// If you store an object which implements this interface inside the context, the completion call will be chained
    /// </summary>
    public interface ICompletable
    {
        Task Complete();
    }

    /// <summary>
    /// If you store an object which implements this interface inside the context, the fault cal will be chained
    /// </summary>
    public interface IFaultable
    {
        Task Fault(Exception exception);

        Task Fault(string reason, Exception exception = null);
    }


    public interface ICancelledToken
    {
        bool IsCancelled { get; }


    }

    public interface ICompletedToken
    {
        bool IsCompleted { get; }
    }

    public interface IFaultedToken
    {
        bool IsFaulted { get; }
    }


    public interface ICompletionToken : ICompletedToken, ICancelledToken, IFaultedToken
    {
        bool CompletionStateIsUnset { get; }
        CancellationToken CancellationToken { get; }
    }

    public interface ICompletionEventRegistrar
    {
        void AddCancallationTask(CancellationTask task);
        void AddCompletionTask(CompletionTask task);
        void AddFaultTask(FaultTask task);
    }

    /// <summary>
    /// Represents an object which can be completed, cancelled, or faulted
    /// </summary>
    public interface ICompletionSource : ICompletable, ICancellable, IFaultable
    {
        Task WaitForCompletion();
    }


//    /// <summary>
//    /// Provides the means to link a <see cref="ICompletionSource"/> to a child <see cref="ICompletionSource"/>
//    /// This is useful when providing a manifold, when one completable task may have many child tasks, and a single fault may not invalidate the set.
//    /// See <see cref="CompletionSource"/> remarks for default implementation.
//    /// </summary>
//    public interface IBranchableCompletionSource : ICompletionSource
//    {
//        IBranchableCompletionSource Branch(BranchedCompletionTask completionTask = null, BranchedCancellationTask cancellationTask = null, BranchedFaultTask faultTask = null);
//    }
}
