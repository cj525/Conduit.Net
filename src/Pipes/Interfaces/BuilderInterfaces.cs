using System;
using System.Threading.Tasks;
using Pipes.Abstraction;
using Pipes.Types;

namespace Pipes.Interfaces
{
    public interface IPipelineBuilder<TContext> : IPipelineConstruction<TContext>, IPipelineExceptionHandler<TContext>, IPipelineInvoker<TContext> where TContext : OperationContext
    {
        void IsImplicitlyWired();
    }

    public interface IPipelineInvoker<TContext> where TContext : OperationContext
    {
        IPipelineInvocation<TContext> IsInvokedBy<TData>(ref Action<TData> trigger) where TData : class;
        IPipelineInvocation<TContext> IsInvokedBy<TData>(ref Action<TData, object> trigger) where TData : class;
        IPipelineInvocation<TContext> IsInvokedAsyncBy<TData>(ref Func<TData, Task> trigger) where TData : class;
        IPipelineInvocation<TContext> IsInvokedAsyncBy<TData>(ref Func<TData, object, Task> trigger) where TData : class;
    }
    public interface IPipelineExceptionHandler<TContext> where TContext : OperationContext
    {
        IPipelineExceptionHandler<TContext> HandlesException<TException>(Func<IPipelineMessage<TContext>, TException, Task> handler) where TException : Exception;
        IPipelineExceptionHandler<TContext> HandlesException<TException>(Action<IPipelineMessage<TContext>, TException> handler) where TException : Exception;
    }

    public interface IPipelineConstruction<TContext> where TContext : OperationContext
    {
        IPipelineConstructor<TContext> Constructs<TComponent>(Func<TComponent> ctor) where TComponent : IPipelineComponent<TContext>;
    }

    public interface IPipelineConstructor<TContext> where TContext : OperationContext
    {
        void Into(ref Stub<TContext> proxy);
    }

    public interface IPipelineInvocation<TContext> where TContext : OperationContext
    {
        void WhichTransmitsTo(Stub<TContext> target);
    }

    public interface IPipelineComponentBuilder<TContext> : IPipelineComponentEmissionBuilder<TContext> where TContext : OperationContext
    {
        IPipelineMessageReceiver<TData,TContext> Receives<TData>() where TData : class;

    }

    public interface IPipelineComponentEmissionBuilder<TContext> where TContext : OperationContext
    {
        IPipelineComponentEmissionBuilder<TContext> Emits<T>() where T : class;
    }

    public interface ICompletionSourceBuilder<out TCompletionEntry, TContext> where TContext : OperationContext where TCompletionEntry : class
    {
        ICompletionSourceBuilder<TCompletionEntry, TContext> WithMaxConcurrency(int max);

        ICompletionSourceBuilder<TCompletionEntry,TContext> OnComplete(Func<IPipelineMessage<TCompletionEntry, TContext>, Task> asyncAction);

        ICompletionSourceBuilder<TCompletionEntry, TContext> OnCancel(Func<IPipelineMessage<TCompletionEntry, TContext>, Task> asyncAction);

        ICompletionSourceBuilder<TCompletionEntry, TContext> OnFault(Func<IPipelineMessage<TCompletionEntry, TContext>, Task> asyncAction);
    }

}
