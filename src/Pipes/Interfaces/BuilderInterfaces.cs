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

        IPipelineConstructorMany<T> ConstructsMany<T>(int count) where T : PipelineComponent;
    }

    public interface IPipelineConstructorBase
    {
        void Into(ref Stub proxy);
    }

    public interface IPipelineConstructor<T> : IPipelineConstructorBase where T : class
    {
        void Using(ref Func<T> ctor);
    }


    // TODO: We still supporting Many?
    public interface IPipelineConstructorManyTarget<out T> where T : class
    {
        IPipelineConstructorManyInitializer<T> Into(ref Stub proxy);
    }

    public interface IPipelineConstructorMany<T> where T : class
    {
        IPipelineConstructorManyTarget<T> Using(Func<T> ctor);
        
    }

    public interface IPipelineConstructorManyInitializer<out T> where T : class
    {
        void WhichAreInitializedWith(Action<T> init);
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
