using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Pipes.Abstraction;

namespace Pipes.Interfaces
{
    public interface IPipelineComponent<TContext> where TContext : class
    {
        void TerminateSource(IPipelineMessage<TContext> message);

        void Build();

        void AttachTo(Pipeline<TContext> pipeline);

        void OnAttach(Action<Pipeline<TContext>> attachAction);
    }

    public interface IPipelineMessage<TContext> where TContext : class
    {
        TContext Context { get; }

        IPipelineComponent<TContext> Sender { get; }

        IEnumerable<IPipelineMessage<TContext>> Stack { get; }

        IEnumerable<object> DataStack { get; }
        
        IPipelineMessage<TContext> Top { get; }

        void Emit<TData>(TData data, TContext context = default(TContext)) where TData : class;

        Task EmitAsync<TData>(TData data, TContext context = default(TContext)) where TData : class;

        void EmitChain<TData>(IPipelineComponent<TContext> origin, TData data, TContext context = default(TContext)) where TData : class;

        Task EmitChainAsync<TData>(IPipelineComponent<TContext> origin, TData data, TContext context = default(TContext)) where TData : class;

        void TerminateSource();
        
        bool RaiseException(Exception exception, TContext context = default(TContext));
    }

    public interface IPipelineMessage<out TData, TContext> : IPipelineMessage<TContext>
        where TData : class
        where TContext : class
    {
        TData Data { get; }
    }

    public interface IPipelineMessageReceiver<out TData, TContext>
        where TData : class
        where TContext : class
    {
        void WhichTriggers(Action action);
        void WhichUnwrapsAndCalls(Action<TData> action);
        void WhichCalls(Action<IPipelineMessage<TData, TContext>> action);

        void WhichTriggersAsync(Func<Task> asyncAction);
        void WhichUnwrapsAndCallsAsync(Func<TData, Task> asyncAction);
        void WhichCallsAsync(Func<IPipelineMessage<TData, TContext>, Task> asyncAction);
    }

    public interface IPipelineMessageSingleTarget<TContext> where TContext : class
    {
        IPipelineConnectorAsync To(Stub<TContext> target);
    }

    public interface IPipelineMessageTap<out TData, TContext>
        where TData : class
        where TContext : class
    {
        IPipelineConnectorAsync WhichTriggers(Action action);
        IPipelineConnectorAsync WhichUnwrapsAndCalls(Action<TData> action);
        IPipelineConnectorAsync WhichCalls(Action<IPipelineMessage<TData, TContext>> action);

        IPipelineConnectorAsync WhichTriggersAsync(Func<Task> asyncAction);
        IPipelineConnectorAsync WhichUnwrapsAndCallsAsync(Func<TData, Task> asyncAction);
        IPipelineConnectorAsync WhichCallsAsync(Func<IPipelineMessage<TData, TContext>, Task> asyncAction);
    }



    public interface IPipelineConnectorBase<TContext> where TContext : class
    {
        IPipelineConnectorAsync SendsMessagesTo(Stub<TContext> target);

        IPipelineMessageSingleTarget<TContext> SendsMessage<TData>() where TData : class;
    }

    public interface IPipelineConnector<TContext> : IPipelineConnectorBase<TContext> where TContext : class
    {
        IPipelineMessageChain<TContext> HasPrivateChannel();

        IPipelineConnectorAsync BroadcastsAllMessages();

        IPipelineConnectorAsync BroadcastsAllMessagesPrivately();
    }

    public interface IPipelineConnectorAsync
    {
        IPipelineConnectorAsyncBuffered OnSeparateThread();

        IPipelineConnectorAsyncWait InParallel();
    }

    public interface IPipelineConnectorAsyncWait
    {
        void WithoutWaiting();
    }

    public interface IPipelineConnectorAsyncBuffered : IPipelineConnectorAsyncWait
    {
        IPipelineConnectorAsyncWait WithQueueLengthOf(int queueLength);
    }

    public interface IPipelineMessageChain<TContext> where TContext : class
    {
        IPipelineConnectorAsync WhichSendsMessagesTo(Stub<TContext> target);

        IPipelineMessageSingleTarget<TContext> WhichSendsMessage<T>() where T : class;
    }
}
