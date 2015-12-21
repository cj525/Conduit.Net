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

        void EmitChain<TData>(IPipelineComponent<TContext> origin, TData data, TContext context = default(TContext)) where TData : class;

        Task EmitChainAsync<TData>(IPipelineComponent<TContext> origin, TData data, TContext context = default(TContext)) where TData : class;
        
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

    public interface IPipelineMessageSingleTarget<TContext> where TContext : class, IOperationContext
    {
        IPipelineConnectorAsyncWithCompletion To(Stub<TContext> target);
    }

    public interface IPipelineMessageSingleTargetWithSubcontext<out TData, TContext> : IPipelineMessageSingleTarget<TContext> where TContext : class, IOperationContext where TData : class
    {
        IPipelineMessageSingleTarget<TContext> WithSubcontext(Func<IPipelineMessage<TData,TContext>, TContext> branchFn);
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
        IPipelineConnectorAsyncWithCompletion SendsMessagesTo(Stub<TContext> target);

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

    public interface IPipelineConnectorWithCompletion
    {
        IPipelineConnectorAsync WithCompletion(int maxConcurrency = 0);
    }

    public interface IPipelineConnectorAsyncWithCompletion : IPipelineConnectorWithCompletion, IPipelineConnectorAsync
    {
        
    }

    public interface IPipelineMessageChain<TContext> where TContext : class, IOperationContext
    {
        IPipelineConnectorAsync WhichSendsMessagesTo(Stub<TContext> target);

        IPipelineMessageSingleTargetWithSubcontext<TData, TContext> WhichSendsMessage<TData>() where TData : class;
    }
}
