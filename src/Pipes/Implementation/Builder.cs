﻿using System;
using System.Threading.Tasks;
using Pipes.Abstraction;
using Pipes.BuildingBlocks;
using Pipes.Interfaces;

namespace Pipes.Implementation
{
    internal class Builder<TContext> : IPipelineBuilder<TContext>, IPipelineComponentBuilder<TContext>
    {
        private readonly IPipelineComponent<TContext> _component;
        private readonly Pipeline<TContext> _pipeline;

        internal Builder(IPipelineComponent<TContext> component)
        {
            _component = component;
        }

        internal Builder(Pipeline<TContext> pipeline)
        {
            _pipeline = pipeline;
        }

        public IPipelineInvocation<TContext> IsInvokedBy<TData>(ref Action<TData, TContext> trigger) where TData : class
        {
            if (_component != null)
            {
                throw new NotSupportedException("Only the pipeline may declare an invocation."); 
                //return new Invocation<T>(_component, ref trigger);
            }
            
            return new Invocation<TData,TContext>(_pipeline, ref trigger);
        }

        public IPipelineInvocation<TContext> IsInvokedAsyncBy<TData>(ref Func<TData, TContext, Task> trigger) where TData : class
        {
            if (_component != null)
            {
                throw new NotSupportedException("Only the pipeline may declare an invocation."); 
                //return new Invocation<T>(_component, ref trigger);
            }

            return new Invocation<TData,TContext>(_pipeline, ref trigger);
        }


        public IPipelineConstructor<TContext> Constructs<TComponent>(Func<TComponent> ctor) where TComponent : IPipelineComponent<TContext>
        {
            if (_component != null)
            {
                throw new NotSupportedException("Only the pipeline may declare a constructor.");
            }

            return new Constructor<TComponent,TContext>(_pipeline, ctor);
        }

        public IPipelineConstructorMany<TContext> ConstructsMany(int count)
        {
            if (_component != null)
            {
                throw new NotSupportedException("Only the pipeline may declare a constructor manifold.");
            }

            return new ConstructorManifold<TContext>(_pipeline,count);
        }

        public IPipelineComponentEmissionBuilder<TContext> Emits<T>() where T : class
        {
            if (_component == null)
                throw new ApplicationException("Can't create emitter on pipeline.");

            Transmitter<T,TContext>.AttachTo(_component);
            
            return this;
        }

        public IPipelineMessageReceiver<T,TContext> Receives<T>() where T : class
        {
            if (_component == null)
                throw new ApplicationException("Can't create receiver on pipeline.");
            
            return new Receiver<T,TContext>(_component);
        }
    }
}
