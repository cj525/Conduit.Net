using System;
using System.Collections.Generic;
using System.Linq;
using Pipes.Implementation;
using Pipes.Interfaces;

namespace Pipes.Abstraction
{
    public abstract class Stub : IPipelineConnector
    {
        protected Pipeline Pipeline;

        protected internal readonly Type ContainedType;
        protected internal PipelineComponent Component;

        private readonly List<Conduit> _tubes = new List<Conduit>();

        protected Stub(PipelineComponent component, Type containedType)
        {
            Component = component;
            ContainedType = containedType;
        }

        internal virtual void AttachTo(Pipeline pipeline)
        {
            Pipeline = pipeline;

            if (_tubes.Any())
                pipeline.AddTubes(_tubes);
        }

        public IPipelineConnectorAsync SendsMessagesTo(Stub target)
        {
            return AddGenericConduit(target);
        }


        public IPipelineMessageSingleTarget<T> SendsMessage<T>() where T : class
        {
            return AddTypedConduit<T>();
        }

        public IPipelineMessageChain HasPrivateChannel()
        {
            return new PrivateTube(this);
        }

        public IPipelineConnectorAsync BroadcastsAllMessages()
        {
            var conduit = new Conduit(this, null);
            _tubes.Add(conduit);
            return conduit;
        }

        public IPipelineConnectorAsync BroadcastsAllMessagesPrivately()
        {
            var conduit = new Conduit(this, null) {IsPrivate = true};
            _tubes.Add(conduit);
            return conduit;
        }

        private IPipelineConnectorAsync AddGenericConduit(Stub target, bool isPrivate = false)
        {
            var conduit = new Conduit(this, target) { IsPrivate = isPrivate };
            _tubes.Add(conduit);
            return conduit;
        }

        private IPipelineMessageSingleTarget<T> AddTypedConduit<T>(bool isPrivate = false) where T : class
        {
            // Create partial conduit
            var conduit = new Conduit.Partial<T>(this) { IsPrivate = isPrivate };
            _tubes.Add(conduit);

            return conduit;
        }

        private class PrivateTube : IPipelineMessageChain
        {
            private readonly Stub _stub;

            internal PrivateTube(Stub stub)
            {
                _stub = stub;
            }

            public IPipelineConnectorAsync WhichSendsMessagesTo(Stub target)
            {
                return _stub.AddGenericConduit(target, true);
            }

            public IPipelineMessageSingleTarget<T> WhichSendsMessage<T>() where T : class
            {
                return _stub.AddTypedConduit<T>(true);
            }
        }
    }
}
