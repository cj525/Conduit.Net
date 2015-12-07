using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pipes.Types;

namespace Pipes.Interfaces
{
    public interface IOperationContext : IDisposable
    {

        void AcquireContextHold();

        OperationContext.DisposableContextHold AcquireDisposableContextHold();
        void ReleaseContextHold();

        CompletionManifold BranchCompletion();

        void Cancel();

        bool IsCancelled { get; }

        bool IsCompleted { get; }

        void Completed();

        void Close();

        bool Contains<T>();

        void MessageCompleted();

        void MessageInFlight();

        void RegisterOnCancellationAction(Action action);

        void RegisterOnCompleteAction(Action action);


        void ApplyOptionalAdjunct<T>(Action<T> operation);


        T Remove<T>();

        T Replace<T>(Func<T, T> func);

        T Retrieve<T>();

        T[] RetrieveDerived<T>();

        void Store<T>() where T : class, new();

        T Store<T>(T adjunct);

        Task WaitForIdle();
    }

    public interface ICancellable
    {
        void Cancel();
    }

    public interface ICompletable
    {
        void Completed();

        bool IsCompleted { get; }

        CompletionManifold Branch(Action completionAction = null);
    }


    public interface ICompletable<out T> : ICompletable
    {
        T Data { get; }
    }
}
