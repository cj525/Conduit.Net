﻿using System;
using System.Threading.Tasks;
using Pipes.Abstraction;

namespace Pipes.Interfaces
{

    public interface IPipelineBuilder<TContext> : IPipelineConstruction<TContext> where TContext : class, IOperationContext
    {
        IPipelineInvocation<TContext> IsInvokedBy<TData>(ref Action<TData> trigger) where TData : class;
        IPipelineInvocation<TContext> IsInvokedBy<TData>(ref Action<TData, object> trigger) where TData : class;
        IPipelineInvocation<TContext> IsInvokedAsyncBy<TData>(ref Func<TData, Task> trigger) where TData : class;
        IPipelineInvocation<TContext> IsInvokedAsyncBy<TData>(ref Func<TData, object, Task> trigger) where TData : class;
    }

    public interface IPipelineConstruction<TContext> where TContext : class, IOperationContext
    {
        IPipelineConstructor<TContext> Constructs<TComponent>(Func<TComponent> ctor) where TComponent : IPipelineComponent<TContext>;
    }

    public interface IPipelineConstructor<TContext> where TContext : class, IOperationContext
    {
        void Into(ref Stub<TContext> proxy);
    }


    // TODO: We still supporting Many?
    public interface IPipelineConstructorManyTarget<TContext> where TContext : class, IOperationContext
    {
        void Into(ref Stub<TContext> proxy);
    }

    public interface IPipelineConstructorMany<TContext> where TContext : class, IOperationContext
    {
        IPipelineConstructorManyTarget<TContext> Using<TComponent>(Func<TComponent> ctor) where TComponent : IPipelineComponent<TContext>;
    }

    public interface IPipelineInvocation<TContext> where TContext : class, IOperationContext
    {
        void WhichTransmitsTo(Stub<TContext> target);
    }

    public interface IPipelineComponentBuilder<TContext> : IPipelineComponentEmissionBuilder<TContext> where TContext : class, IOperationContext
    {
        IPipelineMessageReceiver<TData,TContext> Receives<TData>() where TData : class;

    }

    public interface IPipelineComponentEmissionBuilder<TContext> where TContext : class, IOperationContext
    {
        IPipelineComponentEmissionBuilder<TContext> Emits<T>() where T : class;
    }

    public interface ICompletionSourceBuilder<out TCompletionEntry, TContext> where TContext : class, IOperationContext where TCompletionEntry : class
    {
        ICompletionSourceBuilder<TCompletionEntry, TContext> WithMaxConcurrency(int max);

        ICompletionSourceBuilder<TCompletionEntry,TContext> OnComplete(Func<IPipelineMessage<TCompletionEntry, TContext>, Task> asyncAction);

        ICompletionSourceBuilder<TCompletionEntry, TContext> OnCancel(Func<IPipelineMessage<TCompletionEntry, TContext>, Task> asyncAction);

        ICompletionSourceBuilder<TCompletionEntry, TContext> OnFault(Func<IPipelineMessage<TCompletionEntry, TContext>, Task> asyncAction);
    }

}
