using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Pipes.Abstraction;
using Pipes.Interfaces;

namespace Pipes.Stubs
{
    public abstract class ReceiverStub : Stub
    {
        protected ReceiverStub(PipelineComponent component, Type containedType) : base(component,containedType)
        {
        }

        internal virtual Task Receive(IPipelineMessage message)
        {
            return Task.FromResult(false);
        }

        public class Manifold : ReceiverStub
        {
            internal List<ReceiverStub> Receivers = new List<ReceiverStub>();

            protected Manifold(PipelineComponent component, Type containedType) : base(component, containedType)
            {
            
            }

            internal override Task Receive(IPipelineMessage message)
            {
                return base.Receive(message);
            }
        }


    }

    public class ReceiverStub<T> : ReceiverStub where T : class
    {
        private Func<IPipelineMessage<T>, Task> _bridge = msg => { throw new NotImplementedException("Pipeline Receiver is not attached"); };

        public ReceiverStub(PipelineComponent component) : base(component, typeof(T))
        {
            Pipeline = null;
        }

        public ReceiverStub(Pipeline pipeline) : this(default(PipelineComponent))
        {
            Pipeline = pipeline;
        }


        internal override Task Receive(IPipelineMessage message)
        {
            //Console.WriteLine("- Receiving {0} on Thread {1}", ((IPipelineMessage<object>)message).Data.GetType().Name, Thread.CurrentThread.ManagedThreadId);

            // async keyword not needed, chain maintained on Task
            try
            {
                var pipelineMessage = message as IPipelineMessage<T>;
                if (pipelineMessage == null)
                    throw new Exception("Cast failure in bridge");
                return _bridge(pipelineMessage);
            }
            catch (Exception exception)
            {
                var handled = message.RaiseException(exception);
                if( handled )
                    return Task.FromResult(exception);

                throw;
            }
        }

        internal void Calls(Func<IPipelineMessage<T>, Task> bridge)
        {
            _bridge = bridge;
        }
    }
}