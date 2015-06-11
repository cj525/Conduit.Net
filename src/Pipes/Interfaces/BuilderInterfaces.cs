using System;
using System.Threading.Tasks;
using Pipes.Abstraction;

namespace Pipes.Interfaces
{

    public interface IPipelineBuilder : IPipelineConstruction
    {
        IPipelineInvocation<T> IsInvokedBy<T>(ref Action<T> trigger) where T : class;
        IPipelineInvocation<T> IsInvokedAsyncBy<T>(ref Func<T, Task> trigger) where T : class;
    }

    public interface IPipelineConstruction
    {
        IPipelineConstructor<T> Constructs<T>(Func<T> ctor) where T : PipelineComponent;

        IPipelineConstructorMany ConstructsMany(int count);
    }

    public interface IPipelineConstructor<T>  where T : PipelineComponent
    {
        void Into(ref Stub proxy);
    }


    // TODO: We still supporting Many?
    public interface IPipelineConstructorManyTarget<out T> where T : PipelineComponent
    {
        void Into(ref Stub proxy);
    }

    public interface IPipelineConstructorMany
    {
        IPipelineConstructorManyTarget<T> Using<T>(Func<T> ctor) where T : PipelineComponent;
    }

    public interface IPipelineInvocation<out TArgument> // : IPipelineInvocationBase<TArgument>
    {
        void WhichTransmitsTo(Stub target);
    }

    public interface IPipelineComponentBuilder : IPipelineComponentEmissionBuilder
    {
        IPipelineMessageReceiver<T> Receives<T>() where T : class;

    }

    public interface IPipelineComponentEmissionBuilder
    {
        IPipelineComponentEmissionBuilder Emits<T>() where T : class;
    }


}
