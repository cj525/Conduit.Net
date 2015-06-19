using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Pipes.Abstraction;

namespace Pipes.Interfaces
{

    public interface IPipelineComponent<TScope>
    {
        void TerminateSource(IPipelineMessage<TScope> message);

        void Build();

        void AttachTo(Pipeline<TScope> pipeline);

        void OnAttach(Action<Pipeline<TScope>> attachAction);
    }

    public interface IPipelineMessage<TScope>
    {
        TScope Scope { get; }

        IPipelineComponent<TScope> Sender { get; }

        IEnumerable<IPipelineMessage<TScope>> Stack { get; }

        IEnumerable<object> DataStack { get; }
        
        IPipelineMessage<TScope> Top { get; }

        void Emit<TData>(TData data, TScope scope = default(TScope)) where TData : class;

        Task EmitAsync<TData>(TData data, TScope scope = default(TScope)) where TData : class;

        void EmitChain<TData>(IPipelineComponent<TScope> origin, TData data, TScope scope = default(TScope)) where TData : class;

        Task EmitChainAsync<TData>(IPipelineComponent<TScope> origin, TData data, TScope scope = default(TScope)) where TData : class;

        void TerminateSource();
        
        bool RaiseException(Exception exception, TScope scope = default(TScope));
    }

    public interface IPipelineMessage<out TData, TScope> : IPipelineMessage<TScope> where TData : class
    {
        TData Data { get; }
    }

    public interface IPipelineMessageReceiver<out TData, TScope> where TData : class
    {
        void WhichTriggers(Action action);
        void WhichUnwrapsAndCalls(Action<TData> action);
        void WhichCalls(Action<IPipelineMessage<TData, TScope>> action);

        void WhichTriggersAsync(Func<Task> asyncAction);
        void WhichUnwrapsAndCallsAsync(Func<TData, Task> asyncAction);
        void WhichCallsAsync(Func<IPipelineMessage<TData, TScope>, Task> asyncAction);
    }

    public interface IPipelineMessageSingleTarget<TScope>
    {
        IPipelineConnectorAsync To(Stub<TScope> target);
    }

    public interface IPipelineMessageTap<out TData, TScope> where TData : class
    {
        IPipelineConnectorAsync WhichTriggers(Action action);
        IPipelineConnectorAsync WhichUnwrapsAndCalls(Action<TData> action);
        IPipelineConnectorAsync WhichCalls(Action<IPipelineMessage<TData, TScope>> action);

        IPipelineConnectorAsync WhichTriggersAsync(Func<Task> asyncAction);
        IPipelineConnectorAsync WhichUnwrapsAndCallsAsync(Func<TData, Task> asyncAction);
        IPipelineConnectorAsync WhichCallsAsync(Func<IPipelineMessage<TData, TScope>, Task> asyncAction);
    }



    public interface IPipelineConnectorBase<TScope>
    {
        IPipelineConnectorAsync SendsMessagesTo(Stub<TScope> target);

        IPipelineMessageSingleTarget<TScope> SendsMessage<TData>() where TData : class;
    }

    public interface IPipelineConnector<TScope> : IPipelineConnectorBase<TScope>
    {
        IPipelineMessageChain<TScope> HasPrivateChannel();

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

    public interface IPipelineMessageChain<TScope>
    {
        IPipelineConnectorAsync WhichSendsMessagesTo(Stub<TScope> target);

        IPipelineMessageSingleTarget<TScope> WhichSendsMessage<T>() where T : class;
    }
}
