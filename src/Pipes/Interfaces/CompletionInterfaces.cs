using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pipes.Interfaces
{

    public delegate void CompletionAction();

    public delegate void CancellationAction(string reason = null);

    public delegate void FaultAction(string reason = null, Exception exception = null);



    /// <summary>
    /// If you store an object which implements this interface inside the context, the cancel call will be chained
    /// </summary>
    public interface ICancellable
    {
        void Cancel(string reason = null);

        bool IsCancelled { get; }
    }

    /// <summary>
    /// If you store an object which implements this interface inside the context, the fault cal will be chained
    /// </summary>
    public interface IFaultable
    {
        void Fault(Exception exception);

        void Fault(string reason, Exception exception = null);

        bool IsFaulted { get; }
    }

    /// <summary>
    /// If you store an object which implements this interface inside the context, the completion call will be chained
    /// </summary>
    public interface ICompletable
    {
        void Complete();

        bool IsCompleted { get; }
    }

    /// <summary>
    /// Represents an object which can be completed, cancelled, or faulted
    /// Storing an object which implements this interface inside the context, the completion call will be chained
    /// </summary>
    public interface ICompletionSource : ICompletable, ICancellable, IFaultable
    {

    }

    /// <summary>
    /// Represents an object which holds data of type <typeparam name="T"></typeparam> can be completed, cancelled, or faulted
    /// Storing an object which implements this interface inside the context, the completion call will be chained
    /// </summary>
    public interface ICompletionSource<out T> : ICompletionSource
    {
        T Data { get; }
    }

    /// <summary>
    /// Represents an object which can be completed, cancelled, or faulted, as well as branched
    /// Storing an object which implements this interface inside the context, the completion call will be chained
    /// </summary>
    public interface IBranchableCompletionSource : ICompletionSource
    {
        IBranchableCompletionSource Branch(CompletionAction completionAction = null, CancellationAction cancellationAction = null, FaultAction faultAction = null);
    }
}
