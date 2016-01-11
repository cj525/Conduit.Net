﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Pipes.Abstraction;
using Pipes.BuildingBlocks;
using Pipes.Interfaces;
using Pipes.Types;

namespace Pipes.Implementation
{
    internal class Builder<TContext> : IPipelineBuilder<TContext>, IPipelineComponentBuilder<TContext> where TContext : OperationContext
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

        public void IsImplicitlyWired()
        {
            _pipeline.ImplicitlyWired();
        }
        public IPipelineInvocation<TContext> IsInvokedBy<TData>(ref Action<TData, object> trigger) where TData : class
        {
            if (_component != null)
            {
                throw new NotSupportedException("Only the pipeline may declare an invocation.");
                //return new Invocation<T>(_component, ref trigger);
            }

            return new Invocation<TData, TContext>(_pipeline, ref trigger);
        }
        public IPipelineInvocation<TContext> IsInvokedBy<TData>(ref Action<TData> trigger) where TData : class
        {
            if (_component != null)
            {
                throw new NotSupportedException("Only the pipeline may declare an invocation."); 
                //return new Invocation<T>(_component, ref trigger);
            }
            
            return new Invocation<TData,TContext>(_pipeline, ref trigger);
        }

        public IPipelineInvocation<TContext> IsInvokedAsyncBy<TData>(ref Func<TData, object, Task> trigger) where TData : class
        {
            if (_component != null)
            {
                throw new NotSupportedException("Only the pipeline may declare an invocation.");
                //return new Invocation<T>(_component, ref trigger);
            }

            return new Invocation<TData, TContext>(_pipeline, ref trigger);
        }
        public IPipelineInvocation<TContext> IsInvokedAsyncBy<TData>(ref Func<TData, Task> trigger) where TData : class
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

        public IPipelineComponentEmissionBuilder<TContext> Emits<T>() where T : class
        {
            if (_component == null)
                throw new ApplicationException("Can't create emitter on pipeline.");

            Transmitter<T,TContext>.AttachTo(_component);
            
            return this;
        }

        public IPipelineMessageReceiver<TData,TContext> Receives<TData>() where TData : class
        {
            if (_component == null)
                throw new ApplicationException("Can't create receiver on pipeline.");
            
            return new Receiver<TData,TContext>(_component);
        }

        public IPipelineExceptionHandler<TContext> HandlesException<TException>(Action<IPipelineMessage<TContext>, TException> handler) where TException : Exception
        {
            if (_component != null)
                throw new NotSupportedException("Only the pipeline may declare an exception handler.");

            _pipeline.RegisterExceptionHandler(handler);

            return this;
        }
        public IPipelineExceptionHandler<TContext> HandlesException<TException>(Func<IPipelineMessage<TContext>, TException, Task> handler) where TException : Exception
        {
            if (_component != null)
                throw new NotSupportedException("Only the pipeline may declare an exception handler.");

            _pipeline.RegisterAsyncExceptionHandler(handler);

            return this;
        }
    }
}
