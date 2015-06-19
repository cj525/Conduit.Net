using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Pipes.Abstraction;
using Pipes.Interfaces;

namespace Pipes.Stubs
{
    public abstract class ReceiverStub<TScope> : Stub<TScope>
    {
        protected ReceiverStub(IPipelineComponent<TScope> component, Type containedType) : base(component,containedType)
        {
        }

        [DebuggerHidden]
        internal virtual Task Receive(IPipelineMessage<TScope> message)
        {
            return Task.FromResult(false);
        }

        public class Manifold : ReceiverStub<TScope>
        {
            internal List<ReceiverStub<TScope>> Receivers = new List<ReceiverStub<TScope>>();

            protected Manifold(PipelineComponent<TScope> component, Type containedType)
                : base(component, containedType)
            {
            
            }
        }
    }

    public class ReceiverStub<TData,TScope> : ReceiverStub<TScope> where TData : class
    {
        private Func<IPipelineMessage<TData, TScope>, Task> _bridge = msg => { throw new NotImplementedException("Pipeline Receiver is not attached"); };

        public ReceiverStub(IPipelineComponent<TScope> component) : base(component, typeof(TData))
        {
            Pipeline = null;
        }

        public ReceiverStub(Pipeline<TScope> pipeline) : this(default(PipelineComponent<TScope>))
        {
            Pipeline = pipeline;
        }

        [DebuggerHidden]
        internal override Task Receive(IPipelineMessage<TScope> message)
        {
            //Console.WriteLine("- Receiving {0} on Thread {1}", ((IPipelineMessage<object>)message).Data.GetType().Name, Thread.CurrentThread.ManagedThreadId);

            // async keyword not needed, chain maintained on Task
            try
            {
                var pipelineMessage = message as IPipelineMessage<TData, TScope>;
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

        internal void Calls(Func<IPipelineMessage<TData, TScope>, Task> bridge)
        {
            _bridge = bridge;
        }
    }
}