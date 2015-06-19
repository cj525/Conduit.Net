using System;
using System.Collections.Generic;
using System.Linq;
using Pipes.Implementation;
using Pipes.Interfaces;

namespace Pipes.Abstraction
{
    public abstract class Stub<TScope> : IPipelineConnector<TScope>
    {
        protected Pipeline<TScope> Pipeline;

        protected internal readonly Type ContainedType;
        protected internal readonly IPipelineComponent<TScope> Component;

        private readonly List<Conduit<TScope>> _tubes = new List<Conduit<TScope>>();

        protected Stub(IPipelineComponent<TScope> component, Type containedType)
        {
            Component = component;
            ContainedType = containedType;
        }

        internal virtual void AttachTo(Pipeline<TScope> pipeline)
        {
            Pipeline = pipeline;

            if (_tubes.Any())
                pipeline.AddTubes(_tubes);
        }

        public IPipelineConnectorAsync SendsMessagesTo(Stub<TScope> target)
        {
            return AddGenericConduit(target);
        }


        public IPipelineMessageSingleTarget<TScope> SendsMessage<T>() where T : class
        {
            return AddTypedConduit<T>();
        }

        public IPipelineMessageChain<TScope> HasPrivateChannel()
        {
            return new PrivateTube(this);
        }

        public IPipelineConnectorAsync BroadcastsAllMessages()
        {
            var conduit = new Conduit<TScope>(this, null);
            _tubes.Add(conduit);
            return conduit;
        }

        public IPipelineConnectorAsync BroadcastsAllMessagesPrivately()
        {
            var conduit = new Conduit<TScope>(this, null) { IsPrivate = true };
            _tubes.Add(conduit);
            return conduit;
        }

        private IPipelineConnectorAsync AddGenericConduit(Stub<TScope> target, bool isPrivate = false)
        {
            var conduit = new Conduit<TScope>(this, target) { IsPrivate = isPrivate };
            _tubes.Add(conduit);
            return conduit;
        }

        private IPipelineMessageSingleTarget<TScope> AddTypedConduit<T>(bool isPrivate = false) where T : class
        {
            // Create partial conduit
            var conduit = new Conduit<TScope>.Partial<T>(this) { IsPrivate = isPrivate };
            _tubes.Add(conduit);

            return conduit;
        }

        private class PrivateTube : IPipelineMessageChain<TScope>
        {
            private readonly Stub<TScope> _stub;

            internal PrivateTube(Stub<TScope> stub)
            {
                _stub = stub;
            }

            public IPipelineConnectorAsync WhichSendsMessagesTo(Stub<TScope> target)
            {
                return _stub.AddGenericConduit(target, true);
            }

            public IPipelineMessageSingleTarget<TScope> WhichSendsMessage<T>() where T : class
            {
                return _stub.AddTypedConduit<T>(true);
            }
        }
    }
}
