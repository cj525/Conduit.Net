//using System;
//using System.Collections.Generic;
//using System.Threading;
//using System.Threading.Tasks;
//using Pipes.Implementation;
//using Pipes.Types;

//namespace Pipes.Interfaces
//{

//    // TODO: Rename to IPipelineContext
//    public interface OperationContextz : IDisposable, ICompletionSource, ICompletionToken
//    {
//        // Structure

//        IPipelineComponent[] Components { get; }

//        // Lifetime

//        void AcquireContextHold();

//        OperationContext.DisposableContextHold AcquireDisposableContextHold();

//        void ReleaseContextHold();

//        void Close();

//        void MessageCompleted();

//        void MessageInFlight();

//        // Completion

//        void OnCancellation(CancellationTask task);

//        void OnCompletion(CompletionTask task);

//        void OnFault(FaultTask task);

//        // Error handling

//        void HandleException(Exception exception);

//        bool HasUnhandledException { get; }

//        Exception UnhandledException { get; }

//        // Removable

//        bool ContainsAdjunctAssignableTo<T>();

//        bool ContainsAdjunctOfType<T>();


//        void ApplyOptionalAdjunct<T>(Action<T> operation);

//        T Ensure<T>(Func<T> factory);

//        T Remove<T>();

//        T Replace<T>(Func<T, T> func);

//        T Retrieve<T>();

//        IEnumerable<T> RetrieveDerived<T>();

//        T Store<T>() where T : class, new();

//        T Store<T>(T adjunct);

        
//    }


//}
