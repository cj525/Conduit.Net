using System;
using System.Threading.Tasks;
using Pipes.Abstraction;

namespace Pipes.Interfaces
{

    public interface IPipelineBuilder<TContext> : IPipelineConstruction<TContext> where TContext : class
    {
        IPipelineInvocation<TContext> IsInvokedBy<TData>(ref Action<TData, TContext> trigger) where TData : class;
        IPipelineInvocation<TContext> IsInvokedAsyncBy<TData>(ref Func<TData, TContext, Task> trigger) where TData : class;
    }

    public interface IPipelineConstruction<TContext> where TContext : class
    {
        IPipelineConstructor<TContext> Constructs<TComponent>(Func<TComponent> ctor) where TComponent : IPipelineComponent<TContext>;

        IPipelineConstructorMany<TContext> ConstructsMany(int count);
    }

    public interface IPipelineConstructor<TContext> where TContext : class
    {
        void Into(ref Stub<TContext> proxy);
    }


    // TODO: We still supporting Many?
    public interface IPipelineConstructorManyTarget<TContext> where TContext : class
    {
        void Into(ref Stub<TContext> proxy);
    }

    public interface IPipelineConstructorMany<TContext> where TContext : class
    {
        IPipelineConstructorManyTarget<TContext> Using<TComponent>(Func<TComponent> ctor) where TComponent : IPipelineComponent<TContext>;
    }

    public interface IPipelineInvocation<TContext> where TContext : class
    {
        void WhichTransmitsTo(Stub<TContext> target);
    }

    public interface IPipelineComponentBuilder<TContext> : IPipelineComponentEmissionBuilder<TContext> where TContext : class
    {
        IPipelineMessageReceiver<TData,TContext> Receives<TData>() where TData : class;

    }

    public interface IPipelineComponentEmissionBuilder<TContext> where TContext : class
    {
        IPipelineComponentEmissionBuilder<TContext> Emits<T>() where T : class;
    }


}
