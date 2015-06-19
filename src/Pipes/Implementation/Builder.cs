using System;
using System.Threading.Tasks;
using Pipes.Abstraction;
using Pipes.BuildingBlocks;
using Pipes.Interfaces;

namespace Pipes.Implementation
{
    internal class Builder<TScope> : IPipelineBuilder<TScope>, IPipelineComponentBuilder<TScope>
    {
        private readonly IPipelineComponent<TScope> _component;
        private readonly Pipeline<TScope> _pipeline;

        internal Builder(IPipelineComponent<TScope> component)
        {
            _component = component;
        }

        internal Builder(Pipeline<TScope> pipeline)
        {
            _pipeline = pipeline;
        }

        public IPipelineInvocation<TScope> IsInvokedBy<TData>(ref Action<TData, TScope> trigger) where TData : class
        {
            if (_component != null)
            {
                throw new NotSupportedException("Only the pipeline may declare an invocation."); 
                //return new Invocation<T>(_component, ref trigger);
            }
            
            return new Invocation<TData,TScope>(_pipeline, ref trigger);
        }

        public IPipelineInvocation<TScope> IsInvokedAsyncBy<TData>(ref Func<TData, TScope, Task> trigger) where TData : class
        {
            if (_component != null)
            {
                throw new NotSupportedException("Only the pipeline may declare an invocation."); 
                //return new Invocation<T>(_component, ref trigger);
            }

            return new Invocation<TData,TScope>(_pipeline, ref trigger);
        }


        public IPipelineConstructor<TScope> Constructs<TComponent>(Func<TComponent> ctor) where TComponent : IPipelineComponent<TScope>
        {
            if (_component != null)
            {
                throw new NotSupportedException("Only the pipeline may declare a constructor.");
            }

            return new Constructor<TComponent,TScope>(_pipeline, ctor);
        }

        public IPipelineConstructorMany<TScope> ConstructsMany(int count)
        {
            if (_component != null)
            {
                throw new NotSupportedException("Only the pipeline may declare a constructor manifold.");
            }

            return new ConstructorManifold<TScope>(_pipeline,count);
        }

        public IPipelineComponentEmissionBuilder<TScope> Emits<T>() where T : class
        {
            if (_component == null)
                throw new ApplicationException("Can't create emitter on pipeline.");

            Transmitter<T,TScope>.AttachTo(_component);
            
            return this;
        }

        public IPipelineMessageReceiver<T,TScope> Receives<T>() where T : class
        {
            if (_component == null)
                throw new ApplicationException("Can't create receiver on pipeline.");
            
            return new Receiver<T,TScope>(_component);
        }
    }
}
