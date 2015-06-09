using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Pipes.Abstraction;

namespace Pipes.Interfaces
{
    public interface IPipelineMessage
    {
        //Pipeline Pipeline { get; }
        
        PipelineComponent Sender { get; }

        IEnumerable<IPipelineMessage> Stack { get; }

        IEnumerable<object> DataStack { get; }
        
        IPipelineMessage Top { get; }

        void Emit<T>(T data) where T : class;

        Task EmitAsync<T>(T data) where T : class;

        void EmitChain<T>(PipelineComponent origin, T data) where T : class;

        Task EmitChainAsync<T>(PipelineComponent origin, T data) where T : class;

        void TerminateSource();
        
        bool RaiseException(Exception exception);
    }

    public interface IPipelineMessage<out T> : IPipelineMessage where T : class
    {
        T Data { get; }
    }

    public interface IPipelineMessageReceiver<out T> where T : class
    {
        void WhichTriggers(Action action);
        void WhichUnwrapsAndCalls(Action<T> action);
        void WhichCalls(Action<IPipelineMessage<T>> action);

        void WhichTriggersAsync(Func<Task> asyncAction);
        void WhichUnwrapsAndCallsAsync(Func<T, Task> asyncAction);
        void WhichCallsAsync(Func<IPipelineMessage<T>, Task> asyncAction);
    }

    public interface IPipelineMessageSingleTarget<out T> where T : class
    {
        IPipelineConnectorAsync To(Stub target);
    }

    public interface IPipelineMessageTap<out T> where T : class
    {
        IPipelineConnectorAsync WhichTriggers(Action action);
        IPipelineConnectorAsync WhichUnwrapsAndCalls(Action<T> action);
        IPipelineConnectorAsync WhichCalls(Action<IPipelineMessage<T>> action);

        IPipelineConnectorAsync WhichTriggersAsync(Func<Task> asyncAction);
        IPipelineConnectorAsync WhichUnwrapsAndCallsAsync(Func<T, Task> asyncAction);
        IPipelineConnectorAsync WhichCallsAsync(Func<IPipelineMessage<T>, Task> asyncAction);
    }



    public interface IPipelineConnectorBase
    {
        IPipelineConnectorAsync SendsMessagesTo(Stub target);

        IPipelineMessageSingleTarget<T> SendsMessage<T>() where T : class;
    }

    public interface IPipelineConnector : IPipelineConnectorBase
    {
        IPipelineMessageChain HasPrivateChannel();

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

    public interface IPipelineMessageChain
    {
        IPipelineConnectorAsync WhichSendsMessagesTo(Stub target);

        IPipelineMessageSingleTarget<T> WhichSendsMessage<T>() where T : class;
    }
}
