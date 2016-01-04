using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Pipes.Abstraction;

namespace Pipes.Interfaces
{
    public interface IPipelineComponent<TContext> where TContext : class, IOperationContext
    {
        void Build();

        void AttachTo(Pipeline<TContext> pipeline);

        void OnAttach(Action<Pipeline<TContext>> attachAction);
    }

    public interface IPipelineMessage<TContext> where TContext : class, IOperationContext
    {
        TContext Context { get; }

        IPipelineComponent<TContext> Sender { get; }

        IEnumerable<IPipelineMessage<TContext>> Stack { get; }

        IEnumerable<object> DataStack { get; }
        
        IPipelineMessage<TContext> Top { get; }

        //void Emit<TData>(TData data, TContext context = default(TContext)) where TData : class;

        //Task EmitAsync<TData>(TData data, TContext context = default(TContext)) where TData : class;

        void Chain<TData>(IPipelineComponent<TContext> origin, TData data, TContext subcontext = default(TContext)) where TData : class;

        Task ChainAsync<TData>(IPipelineComponent<TContext> origin, TData data, TContext subcontext = default(TContext)) where TData : class;

        bool HandleException(Exception exception);
    }

    public interface IPipelineMessage<out TData, TContext> : IPipelineMessage<TContext>
        where TData : class
        where TContext : class, IOperationContext
    {
        TData Data { get; }
    }

    public interface IPipelineMessageReceiver<out TData, TContext>
        where TData : class
        where TContext : class, IOperationContext
    {
        void WhichTriggers(Action action);
        void WhichUnwrapsAndCalls(Action<TData> action);
        void WhichCalls(Action<IPipelineMessage<TData, TContext>> action);

        void WhichTriggersAsync(Func<Task> asyncAction);
        void WhichUnwrapsAndCallsAsync(Func<TData, Task> asyncAction);
        void WhichCallsAsync(Func<IPipelineMessage<TData, TContext>, Task> asyncAction);
    }

    public interface IPipelineMessageSingleTarget<out TData, TContext> where TContext : class, IOperationContext where TData : class
    {
        IPipelineConnectorWithCompletion To(Stub<TContext> target);
    }

    public interface IPipelineMessageSingleTargetWithSubcontext<out TData, TContext> : IPipelineMessageSingleTarget<TContext> where TContext : class, IOperationContext where TData : class
    {
        IPipelineMessageSingleTarget<TContext> WithSubcontext(Func<IPipelineMessage<TData,TContext>, TContext> branchFn);
    }

    public interface IPipelineMessageTap<out TData, TContext>
        where TData : class
        where TContext : class, IOperationContext
    {
        IPipelineConnector WhichTriggers(Action action);
        IPipelineConnector WhichUnwrapsAndCalls(Action<TData> action);
        IPipelineConnector WhichCalls(Action<IPipelineMessage<TData, TContext>> action);

        IPipelineConnector WhichTriggersAsync(Func<Task> asyncAction);
        IPipelineConnector WhichUnwrapsAndCallsAsync(Func<TData, Task> asyncAction);
        IPipelineConnector WhichCallsAsync(Func<IPipelineMessage<TData, TContext>, Task> asyncAction);
    }



    public interface IPipelineConnectorBase<TContext> where TContext : class, IOperationContext
    {
        IPipelineConnectorWithCompletion SendsMessagesTo(Stub<TContext> target);

        IPipelineMessageSingleTargetWithSubcontext<TData, TContext> SendsMessage<TData>() where TData : class;
    }

    public interface IPipelineConnector<TContext> : IPipelineConnectorBase<TContext> where TContext : class, IOperationContext
    {
        IPipelineMessageChain<TContext> HasPrivateChannel();

        IPipelineConnector BroadcastsAllMessages();

        IPipelineConnector BroadcastsAllMessagesPrivately();
    }

    public interface IPipelineConnector
    {
        IPipelineConnectorBuffered OnSeparateThread();

        void InParallel();
    }

    public interface IPipelineConnectorBuffered
    {
        void WithQueueLengthOf(int queueLength);
    }

    public interface IPipelineConnectorWithCompletion
    {
        IPipelineConnectorAsync WithCompletion(int maxConcurrency = 0);
        IPipelineConnectorAsync WithCompletion(Action<IPipelineCompletion<TData, TContext>> 
    }

    //public interface IPipelineConnectorAsyncWithCompletion : IPipelineConnectorWithCompletion, IPipelineConnector
    //{
        
    //}

    public interface IPipelineMessageChain<TContext> where TContext : class, IOperationContext
    {
        IPipelineConnector WhichSendsMessagesTo(Stub<TContext> target);

        IPipelineMessageSingleTargetWithSubcontext<TData, TContext> WhichSendsMessage<TData>() where TData : class;
    }

    public interface IPipelineCompletion<out TData, TContext> : IPipelineCompletionCallbacks<TData, TContext> where TContext : class, IOperationContext where TData : class 
    {
        IPipelineCompletionCallbacks<TData,TContext> WithMaximumConcurrency(int maxConcurrency);

        IPipelineCompletionCallbacks<TData, TContext> WithUnlimitedConcurrency(int maxConcurrency);
    }

    public interface IPipelineCompletionCallbacks<out TData,TContext> where TContext : class, IOperationContext where TData : class
    {
        IPipelineCompletionCallbacks<TData,TContext> OnCompletion(Action<IPipelineMessage<TData,TContext>> messageCompletionAction);

        IPipelineCompletionCallbacks<TData, TContext> OnCancellation(Action<IPipelineMessage<TData, TContext>> messageCancellationAction);

        IPipelineCompletionCallbacks<TData, TContext> OnFault(Action<IPipelineMessage<TData, TContext>> messageFaultAction);
    }
}
