using System;
using System.Threading.Tasks;
using Pipes.Abstraction;
using Pipes.Interfaces;
using Pipes.Types;

namespace Pipes.Stubs
{
    public abstract class InvocationStub<TContext> : Stub<TContext> where TContext : class, IOperationContext
    {
        internal Stub<TContext> Target;

        protected InvocationStub(Type containedType)
            : base(null, containedType)
        {
            
        }
    }

    public class InvocationStub<TData, TContext> : InvocationStub<TContext>
        where TData : class
        where TContext : class, IOperationContext
    {

        public InvocationStub() : base(typeof(TData))
        {
        }

        private async Task Trigger(TData data, TContext context) 
        {
            var message = new PipelineMessage<TData,TContext>(Pipeline, Component, data, context);
            await Pipeline.RouteMessage(message);
        }

        // ReSharper disable once RedundantAssignment
        internal void GetTrigger(ref Action<TData,TContext> trigger)
        {
            trigger = (data, token) => Trigger(data,token).Wait();
        }

        // ReSharper disable once RedundantAssignment
        internal void GetAsyncTrigger(ref Func<TData, TContext, Task> trigger)
        {
            trigger = Trigger;
        }

        internal void SetTarget(Stub<TContext> target)
        {
            // IsConstructed = false
            Target = target;
        }

        //internal void SetConstructor<TInstance>(Func<T, TInstance> ctor)
        //{
        //    IsConstructed = true;
        //    Func<T, TInstance> hook = (T arg) =>
        //    {
        //        var instance = ctor(arg);
        //        var component = instance as PipelineComponent;
        //        if (component != null)
        //        {
        //            component.Build();
        //            component.AttachTo(Pipeline);
        //            Pipeline.AttachComponent(component);
                    
        //            // Hack, changes made after startup
        //            Pipeline.AttachRxTx();
        //            Pipeline.LinkChannels();
        //        }

        //        return instance;
        //    };

        //    _target = new Target<TInstance>(hook, ref _trigger);
        //}



        //internal void ApplyTarget<TInstance>(Action<Target<TInstance>> action)
        //{
        //    action((Target<TInstance>) _target);
        //}

        //internal class Target<TInstance> : Target
        //{
        //    private readonly Func<T, TInstance> _ctor;
        //    private Func<TInstance, Task> _trigger;
        //    private Action<TInstance> _init;
        //    private TInstance _instance;

        //    // ReSharper disable once RedundantAssignment
        //    public Target(Func<T, TInstance> ctor, ref Func<T,Task> trigger)
        //    {
        //        _ctor = ctor;
        //        trigger = Trigger;
        //    }
        //    // TODO: Clean up the anons
        //    internal void SetTrigger(Action<TInstance> trigger)
        //    {
        //        _trigger = data =>
        //        {
        //            trigger(data);

        //            return EmptyTask;
        //        };
        //    }
        //    internal void SetAsyncTrigger(Func<TInstance, Task> trigger)
        //    {
        //        _trigger = trigger;
        //    }

        //    internal void SetInitializer(Action<TInstance> init)
        //    {
        //        _init = init;
        //    }

        //    private Task Trigger(T data)
        //    {
        //        // TODO: Register instance for disposal?
        //        _instance = _ctor(data);

        //        if (_init != null)
        //            _init(_instance);

        //        return _trigger(_instance);
        //    }
        //}
    }
}