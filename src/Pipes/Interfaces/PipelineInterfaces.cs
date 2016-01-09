using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Pipes.Abstraction;
using Pipes.Types;

namespace Pipes.Interfaces
{
    public interface IPipelineComponent : IDisposable
    {
        
    }
    public interface IPipelineComponent<TContext> : IPipelineComponent where TContext : OperationContext
    {
        void Build();

        void AttachTo(Pipeline<TContext> pipeline);

        void OnAttach(Action<Pipeline<TContext>> attachAction);
    }

    public interface IPipelineConstructorStub<TContext> where TContext : OperationContext
    {
        IPipelineComponent<TContext> Construct();
    }


    public interface IPipelineMessage<TContext> where TContext : OperationContext
    {
        TContext Context { get; }

        IPipelineComponent<TContext> Sender { get; }

        IEnumerable<IPipelineMessage<TContext>> Stack { get; }

        IEnumerable<IPipelineMessage<TContext>> FullStack { get; }

        IEnumerable<object> DataStack { get; }

        IEnumerable<object> MetaStack { get; }

        object Meta { get; }

        void Chain<TData>(IPipelineComponent<TContext> origin, TData data, object meta = null) where TData : class;

        //Task ChainAsync<TData>(IPipelineComponent<TContext> origin, TData data, object meta = null) where TData : class;
    }

    public interface IPipelineMessage<out TData, TContext> : IPipelineMessage<TContext>, IReadableData<TData>
        where TData : class
        where TContext : OperationContext
    {
    }

    public interface IReadableData<out TData> where TData : class
    {
        TData Data { get; }
    }

    public interface IWritableData<in TData> where TData : class
    {
        TData Data { set; }
    }

    public interface IPipelineMessageReceiver<out TData, TContext>
           where TData : class
           where TContext : OperationContext
    {
        void WhichTriggers(Action action);
        void WhichUnwrapsAndCalls(Action<TData> action);
        void WhichCalls(Action<IPipelineMessage<TData, TContext>> action);

        void WhichTriggersAsync(Func<Task> asyncAction);
        void WhichUnwrapsAndCallsAsync(Func<TData, Task> asyncAction);
        void WhichCallsAsync(Func<IPipelineMessage<TData, TContext>, Task> asyncAction);
    }

    public interface IPipelineMessageSingleTarget<TContext> where TContext : OperationContext
    {
        void To(Stub<TContext> target);
    }

    public interface IPipelineMessageTap<out TData, TContext>
        where TData : class
        where TContext : OperationContext
    {
        IPipelineConnectorAsync WhichTriggers(Action action);
        IPipelineConnectorAsync WhichUnwrapsAndCalls(Action<TData> action);
        IPipelineConnectorAsync WhichCalls(Action<IPipelineMessage<TData, TContext>> action);

        IPipelineConnectorAsync WhichTriggersAsync(Func<Task> asyncAction);
        IPipelineConnectorAsync WhichUnwrapsAndCallsAsync(Func<TData, Task> asyncAction);
        IPipelineConnectorAsync WhichCallsAsync(Func<IPipelineMessage<TData, TContext>, Task> asyncAction);
    }



    public interface IPipelineConnectorBase<TContext> where TContext : OperationContext
    {
        void SendsMessagesTo(Stub<TContext> target);

        IPipelineMessageSingleTarget<TContext> SendsMessage<TData>() where TData : class;
    }

    public interface IPipelineConnector<TContext> : IPipelineConnectorBase<TContext> where TContext : OperationContext
    {
        IPipelineMessageChain<TContext> HasPrivateChannel();

        void BroadcastsAllMessages();

        void BroadcastsAllMessagesPrivately();
    }

    public interface IPipelineConnectorAsync
    {
        IPipelineConnectorOnSeparateThread OnSeparateThread();

        IPipelineConnectorInParallel InParallel();
    }

    public interface IPipelineConnectorOnSeparateThread
    {
        void InParallel();
    }
    public interface IPipelineConnectorInParallel
    {
        void OnSeparateThread();
    }
    public interface IPipelineMessageChain<TContext> where TContext : OperationContext
    {
        void WhichSendsMessagesTo(Stub<TContext> target);

        IPipelineMessageSingleTarget<TContext> WhichSendsMessage<T>() where T : class;
    }
}
