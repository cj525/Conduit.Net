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
        IPipelineConstructor<T> Constructs<T>(Func<T> ctor) where T : class;

        IPipelineConstructorManyBase<T> ConstructsMany<T>() where T : class;
    }


    public interface IPipelineConstructorBase
    {
        void Into(ref Stub proxy);
    }

    public interface IPipelineSupportConstructor<T> where T : class
    {
        void Once();
    }

    public interface IPipelineConstructor<T> : IPipelineConstructorBase, IPipelineSupportConstructor<T> where T : class
    {
        /// <summary>
        /// As opposed to new every time or once-forever (not supported).
        /// New every time is the default behavior.
        /// </summary>
        /// <returns></returns>
        IPipelineSupportConstructor<T> Using(ref Func<T> ctor);
    }


    // TODO: We still supporting Many?
    public interface IPipelineConstructorManyBase<T> where T : class
    {
        IPipelineConstructorMany<T> Using(Func<T> ctor);
    }

    public interface IPipelineConstructorMany<T> : IPipelineConstructorManyBase<T> where T : class
    {
        IPipelineConstructorManyInitializer<T> Into(ref Stub proxy);
    }

    public interface IPipelineConstructorManyInitializer<out T> where T : class
    {
        void WhichAreInitializedWith(Action<T> init);
    }


    public interface IPipelineInvocation<out TArgument> // : IPipelineInvocationBase<TArgument>
    {
        void WhichTransmitsTo(Stub target);
    }

    public interface IPipelineComponentBuilder : IPipelineConstruction, IPipelineComponentEmissionBuilder
    {
        IPipelineMessageReceiver<T> Receives<T>() where T : class;

    }

    public interface IPipelineComponentEmissionBuilder
    {
        IPipelineComponentEmissionBuilder Emits<T>() where T : class;
    }


}
