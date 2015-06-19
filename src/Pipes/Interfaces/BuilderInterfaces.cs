using System;
using System.Threading.Tasks;
using Pipes.Abstraction;

namespace Pipes.Interfaces
{

    public interface IPipelineBuilder<TScope> : IPipelineConstruction<TScope>
    {
        IPipelineInvocation<TScope> IsInvokedBy<TData>(ref Action<TData, TScope> trigger) where TData : class;
        IPipelineInvocation<TScope> IsInvokedAsyncBy<TData>(ref Func<TData, TScope, Task> trigger) where TData : class;
    }

    public interface IPipelineConstruction<TScope>
    {
        IPipelineConstructor<TScope> Constructs<T>(Func<T> ctor) where T : IPipelineComponent<TScope>;

        IPipelineConstructorMany<TScope> ConstructsMany(int count);
    }

    public interface IPipelineConstructor<TScope>
    {
        void Into(ref Stub<TScope> proxy);
    }


    // TODO: We still supporting Many?
    public interface IPipelineConstructorManyTarget<out TData, TScope> where TData : IPipelineComponent<TScope>
    {
        void Into(ref Stub<TScope> proxy);
    }

    public interface IPipelineConstructorMany<TScope>
    {
        IPipelineConstructorManyTarget<TComponent,TScope> Using<TComponent>(Func<TComponent> ctor) where TComponent : IPipelineComponent<TScope>;
    }

    public interface IPipelineInvocation<TScope>
    {
        void WhichTransmitsTo(Stub<TScope> target);
    }

    public interface IPipelineComponentBuilder<TScope> : IPipelineComponentEmissionBuilder<TScope>
    {
        IPipelineMessageReceiver<TData,TScope> Receives<TData>() where TData : class;

    }

    public interface IPipelineComponentEmissionBuilder<TScope>
    {
        IPipelineComponentEmissionBuilder<TScope> Emits<T>() where T : class;
    }


}
