using System;
using System.Collections.Generic;
using System.Linq;
using Pipes.Implementation;
using Pipes.Interfaces;
using Pipes.Types;

namespace Pipes.Abstraction
{
    public abstract class Stub<TContext> : IPipelineConnector<TContext> where TContext : OperationContext
    {
        protected Pipeline<TContext> Pipeline;

        protected internal readonly Type ContainedType;
        protected internal readonly IPipelineComponent<TContext> Component;

        internal readonly List<Route<TContext>> Routes = new List<Route<TContext>>();

        protected Stub(IPipelineComponent<TContext> component, Type containedType)
        {
            Component = component;
            ContainedType = containedType;
        }

        internal virtual void AttachTo(Pipeline<TContext> pipeline)
        {
            Pipeline = pipeline;

            if (Routes.Any())
                pipeline.AddRoutes(Routes);
        }

        public void SendsMessagesTo(Stub<TContext> target)
        {
            AddGenericRoute(target);
        }


        public IPipelineMessageSingleTarget<TContext> SendsMessage<T>() where T : class
        {
            return AddTypedRoute<T>();
        }

        public IPipelineMessageChain<TContext> HasPrivateChannel()
        {
            return new PrivateRoute(this);
        }

        public void BroadcastsAllMessages()
        {
            var route = new Route<TContext>(this, null);
            Routes.Add(route);
        }

        public void BroadcastsAllMessagesPrivately()
        {
            var route = new Route<TContext>(this, null) { IsPrivate = true };
            Routes.Add(route);
        }

        private void AddGenericRoute(Stub<TContext> target, bool isPrivate = false)
        {
            var route = new Route<TContext>(this, target) { IsPrivate = isPrivate };
            Routes.Add(route);
        }

        private IPipelineMessageSingleTarget<TContext> AddTypedRoute<T>(bool isPrivate = false) where T : class
        {
            // Create partial conduit
            var route = new Route<TContext>.Partial<T>(this) { IsPrivate = isPrivate };
            Routes.Add(route);
            return route;
        }


        private class PrivateRoute : IPipelineMessageChain<TContext>
        {
            private readonly Stub<TContext> _stub;

            internal PrivateRoute(Stub<TContext> stub)
            {
                _stub = stub;
            }

            public void WhichSendsMessagesTo(Stub<TContext> target)
            {
                _stub.AddGenericRoute(target, true);
            }

            public IPipelineMessageSingleTarget<TContext> WhichSendsMessage<T>() where T : class
            {
                return _stub.AddTypedRoute<T>(true);
            }
        }
    }
}
