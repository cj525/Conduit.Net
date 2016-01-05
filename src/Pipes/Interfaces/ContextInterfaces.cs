using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Pipes.Types;

namespace Pipes.Interfaces
{

    // TODO: Rename to IPipelineContext
    public interface IOperationContext : IDisposable, ICompletionSource, ICompletionToken
    {
        void AcquireContextHold();

        OperationContext.DisposableContextHold AcquireDisposableContextHold();

        void ReleaseContextHold();

        void Close();

        void MessageCompleted();

        void MessageInFlight();

        void RegisterOnCancellation(CancellationTask task);

        void RegisterOnCompletion(CompletionTask task);

        void RegisterOnFault(FaultTask task);


        bool ContainsAdjunctAssignableTo<T>();

        bool ContainsAdjunctOfType<T>();


        void ApplyOptionalAdjunct<T>(Action<T> operation);

        T Ensure<T>(Func<T> factory);

        T Remove<T>();

        T Replace<T>(Func<T, T> func);

        T Retrieve<T>();

        IEnumerable<T> RetrieveDerived<T>();

        T Store<T>() where T : class, new();

        T Store<T>(T adjunct);

        
    }


}
