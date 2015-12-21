using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Pipes.Types;

namespace Pipes.Interfaces
{

    // TODO: Rename to IPipelineContext
    public interface IOperationContext : IDisposable, ICompletionSource
    {
        void AcquireContextHold();

        OperationContext.DisposableContextHold AcquireDisposableContextHold();

        void ReleaseContextHold();

        CompletionManifold BranchCompletion();

        void Close();

        void MessageCompleted();

        void MessageInFlight();

        void RegisterOnCancellation(CancellationTask action);

        void RegisterOnCompletion(CompletionTask action);

        void RegisterOnFault(FaultTask action);


        bool ContainsAdjunctAssignableTo<T>();

        bool ContainsAdjunctOfType<T>();


        void ApplyOptionalAdjunct<T>(Action<T> operation);


        T Remove<T>();

        T Replace<T>(Func<T, T> func);

        T Retrieve<T>();

        IEnumerable<T> RetrieveDerived<T>();

        void Store<T>() where T : class, new();

        T Store<T>(T adjunct);

        Task WaitForIdle(int waitTimeSliceMs);
    }


}
