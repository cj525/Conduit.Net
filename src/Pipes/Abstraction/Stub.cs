using System;
using System.Collections.Generic;
using System.Linq;
using Pipes.Implementation;
using Pipes.Interfaces;

namespace Pipes.Abstraction
{
    public abstract class Stub<TContext> : IPipelineConnector<TContext> where TContext : class, IOperationContext
    {
        protected Pipeline<TContext> Pipeline;

        protected internal readonly Type ContainedType;
        protected internal readonly IPipelineComponent<TContext> Component;

        private readonly List<Conduit<TContext>> _tubes = new List<Conduit<TContext>>();

        protected Stub(IPipelineComponent<TContext> component, Type containedType)
        {
            Component = component;
            ContainedType = containedType;
        }

        internal virtual void AttachTo(Pipeline<TContext> pipeline)
        {
            Pipeline = pipeline;

            if (_tubes.Any())
                pipeline.AddTubes(_tubes);
        }

        public IPipelineConnectorAsyncWithCompletion SendsMessagesTo(Stub<TContext> target)
        {
            return AddGenericConduit(target);
        }


        public IPipelineMessageSingleTargetWithSubcontext<T,TContext> SendsMessage<T>() where T : class
        {
            return AddTypedConduit<T>();
        }

        public IPipelineMessageChain<TContext> HasPrivateChannel()
        {
            return new PrivateTube(this);
        }

        public IPipelineConnectorAsync BroadcastsAllMessages()
        {
            var conduit = new Conduit<TContext>(this, null);
            _tubes.Add(conduit);
            return conduit;
        }

        public IPipelineConnectorAsync BroadcastsAllMessagesPrivately()
        {
            var conduit = new Conduit<TContext>(this, null) { IsPrivate = true };
            _tubes.Add(conduit);
            return conduit;
        }

        private IPipelineConnectorAsyncWithCompletion AddGenericConduit(Stub<TContext> target, bool isPrivate = false)
        {
            var conduit = new Conduit<TContext>(this, target) { IsPrivate = isPrivate };
            _tubes.Add(conduit);
            return conduit;
        }

        private IPipelineMessageSingleTargetWithSubcontext<T,TContext> AddTypedConduit<T>(bool isPrivate = false) where T : class
        {
            // Create partial conduit
            var conduit = new Conduit<TContext>.Partial<T>(this) { IsPrivate = isPrivate };
            _tubes.Add(conduit);

            return conduit;
        }


        private class PrivateTube : IPipelineMessageChain<TContext>
        {
            private readonly Stub<TContext> _stub;

            internal PrivateTube(Stub<TContext> stub)
            {
                _stub = stub;
            }

            public IPipelineConnectorAsync WhichSendsMessagesTo(Stub<TContext> target)
            {
                return _stub.AddGenericConduit(target, true);
            }

            public IPipelineMessageSingleTargetWithSubcontext<T,TContext> WhichSendsMessage<T>() where T : class
            {
                return _stub.AddTypedConduit<T>(true);
            }
        }
    }
}
