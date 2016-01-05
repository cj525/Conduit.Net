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

        IEnumerable<IPipelineMessage<TContext>> FullStack { get; }

        IEnumerable<object> DataStack { get; }

        IEnumerable<object> MetaStack { get; }

        object Meta { get; }

        void Chain<TData>(IPipelineComponent<TContext> origin, TData data, object meta = null) where TData : class;

        Task ChainAsync<TData>(IPipelineComponent<TContext> origin, TData data, object meta = null) where TData : class;

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
        IPipelineConnectorAsync To(Stub<TContext> target);
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

        IPipelineMessageSingleTarget<TContext> SendsMessage<TData>() where TData : class;
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

    public interface IPipelineMessageChain<TContext> where TContext : class, IOperationContext
    {
        IPipelineConnectorAsync WhichSendsMessagesTo(Stub<TContext> target);

        IPipelineMessageSingleTarget<TContext> WhichSendsMessage<T>() where T : class;
    }
}
