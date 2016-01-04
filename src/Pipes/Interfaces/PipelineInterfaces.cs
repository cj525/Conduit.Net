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
        IPipelineConnectorAsyncWithCompletion<TData, TContext> To(Stub<TContext> target);
    }

    public interface IPipelineMessageSingleTargetWithSubcontext<out TData, TContext> : IPipelineMessageSingleTarget<TData, TContext> where TContext : class, IOperationContext where TData : class
    {
        IPipelineMessageSingleTarget<TData, TContext> WithSubcontext<TSourceContext>(Func<IPipelineMessage<TData, TSourceContext>, TContext> subcontextFactory) where TSourceContext : class, IOperationContext;
    }

    public interface IPipelineMessageTap<out TData, TContext>
        where TData : class
        where TContext : class, IOperationContext
    {
        IPipelineConnectorAsync WhichTriggers(Action action);
        IPipelineConnectorAsync WhichUnwrapsAndCalls(Action<TData> action);
        IPipelineConnectorAsync WhichCalls(Action<IPipelineMessage<TData, TContext>> action);

        IPipelineConnectorAsync WhichTriggersAsync(Func<Task> asyncAction);
        IPipelineConnectorAsync WhichUnwrapsAndCallsAsync(Func<TData, Task> asyncAction);
        IPipelineConnectorAsync WhichCallsAsync(Func<IPipelineMessage<TData, TContext>, Task> asyncAction);
    }



    public interface IPipelineConnectorBase<TContext> where TContext : class, IOperationContext
    {
        IPipelineConnectorAsync SendsMessagesTo(Stub<TContext> target);

        IPipelineMessageSingleTargetWithSubcontext<TData, TContext> SendsMessage<TData>() where TData : class;
    }

    public interface IPipelineConnector<TContext> : IPipelineConnectorBase<TContext> where TContext : class, IOperationContext
    {
        IPipelineMessageChain<TContext> HasPrivateChannel();

        IPipelineConnectorAsync BroadcastsAllMessages();

        IPipelineConnectorAsync BroadcastsAllMessagesPrivately();
    }

    public interface IPipelineConnectorAsync
    {
        IPipelineConnectorAsyncBuffered OnSeparateThread();

        void InParallel();
    }

    public interface IPipelineConnectorAsyncBuffered
    {
        void WithQueueLengthOf(int queueLength);
    }

    public interface IPipelineConnectorWithCompletion<out TData, TContext> where TContext : class, IOperationContext where TData : class
    {
        IPipelineConnectorAsync WithCompletion(int maxConcurrency = 0);
        IPipelineConnectorAsync WithCompletion(Action<IPipelineCompletion<TData, TContext>> initializer);
    }

    public interface IPipelineConnectorAsyncWithCompletion<out TData, TContext> : IPipelineConnectorWithCompletion<TData, TContext>, IPipelineConnectorAsync where TContext : class, IOperationContext where TData : class
    {
        
    }

    public interface IPipelineMessageChain<TContext> where TContext : class, IOperationContext
    {
        IPipelineConnectorAsync WhichSendsMessagesTo(Stub<TContext> target);

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
