using System;
using System.Threading.Tasks;
using Pipes.Abstraction;
using Pipes.BuildingBlocks;
using Pipes.Interfaces;

namespace Pipes.Implementation
{
    internal class Builder : IPipelineBuilder, IPipelineComponentBuilder
    {
        private readonly PipelineComponent _component;
        private readonly Pipeline _pipeline;

        internal Builder(PipelineComponent component)
        {
            _component = component;
        }

        internal Builder(Pipeline pipeline)
        {
            _pipeline = pipeline;
        }

        public IPipelineInvocation<T> IsInvokedBy<T>(ref Action<T> trigger) where T : class
        {
            if (_component != null)
            {
                throw new NotSupportedException("Only the pipeline may declare an invocation."); 
                //return new Invocation<T>(_component, ref trigger);
            }
            
            return new Invocation<T>(_pipeline, ref trigger);
        }

        public IPipelineInvocation<T> IsInvokedAsyncBy<T>(ref Func<T, Task> trigger) where T : class
        {
            if (_component != null)
            {
                throw new NotSupportedException("Only the pipeline may declare an invocation."); 
                //return new Invocation<T>(_component, ref trigger);
            }

            return new Invocation<T>(_pipeline, ref trigger);
        }


        public IPipelineConstructor<T> Constructs<T>(Func<T> ctor) where T : class
        {
            if (_component != null)
            {
                return new Constructor<T>(_component, ctor);
            }

            return new Constructor<T>(_pipeline, ctor);
        }

        public IPipelineConstructorManyBase<T> ConstructsMany<T>() where T : class
        {
            if (_component != null)
            {
                return new ConstructorManifold<T>(_component);
            }

            return new ConstructorManifold<T>(_pipeline);
        }

        public IPipelineComponentEmissionBuilder Emits<T>() where T : class
        {
            if (_component == null)
                throw new ApplicationException("Can't create emitter on pipeline.");

            Transmitter<T>.AttachTo(_component);
            
            return this;
        }

        public IPipelineMessageReceiver<T> Receives<T>() where T : class
        {
            if (_component == null)
                throw new ApplicationException("Can't create receiver on pipeline.");
            
            return new Receiver<T>(_component);
        }
    }
}
