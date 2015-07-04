using System;
using System.Threading.Tasks;
using Pipes.Abstraction;

namespace Pipes.Interfaces
{

    public interface IPipelineBuilder<TContext> : IPipelineConstruction<TContext>
    {
        IPipelineInvocation<TContext> IsInvokedBy<TData>(ref Action<TData, TContext> trigger) where TData : class;
        IPipelineInvocation<TContext> IsInvokedAsyncBy<TData>(ref Func<TData, TContext, Task> trigger) where TData : class;
    }

    public interface IPipelineConstruction<TContext>
    {
        IPipelineConstructor<TContext> Constructs<TComponent>(Func<TComponent> ctor) where TComponent : IPipelineComponent<TContext>;

        IPipelineConstructorMany<TContext> ConstructsMany(int count);
    }

    public interface IPipelineConstructor<TContext>
    {
        void Into(ref Stub<TContext> proxy);
    }


    // TODO: We still supporting Many?
    public interface IPipelineConstructorManyTarget<TContext>
    {
        void Into(ref Stub<TContext> proxy);
    }

    public interface IPipelineConstructorMany<TContext>
    {
        IPipelineConstructorManyTarget<TContext> Using<TComponent>(Func<TComponent> ctor) where TComponent : IPipelineComponent<TContext>;
    }

    public interface IPipelineInvocation<TContext>
    {
        void WhichTransmitsTo(Stub<TContext> target);
    }

    public interface IPipelineComponentBuilder<TContext> : IPipelineComponentEmissionBuilder<TContext>
    {
        IPipelineMessageReceiver<TData,TContext> Receives<TData>() where TData : class;

    }

    public interface IPipelineComponentEmissionBuilder<TContext>
    {
        IPipelineComponentEmissionBuilder<TContext> Emits<T>() where T : class;
    }


}
