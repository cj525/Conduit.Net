using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Pipes.Abstraction;
using Pipes.Interfaces;

namespace Pipes.Stubs
{
    public abstract class ReceiverStub<TContext> : Stub<TContext> where TContext : class, IOperationContext
    {
        protected ReceiverStub(IPipelineComponent<TContext> component, Type containedType) : base(component,containedType)
        {
        }

        [DebuggerHidden]
        internal virtual Task Receive(IPipelineMessage<TContext> message)
        {
            return Task.FromResult(false);
        }
    }

    public class ReceiverStub<TData, TContext> : ReceiverStub<TContext>
        where TData : class
        where TContext : class, IOperationContext
    {
        private Func<IPipelineMessage<TData, TContext>, Task> _bridge = msg => { throw new NotImplementedException("Pipeline Receiver is not attached"); };

        public ReceiverStub(IPipelineComponent<TContext> component) : base(component, typeof(TData))
        {
            Pipeline = null;
        }

        public ReceiverStub(Pipeline<TContext> pipeline) : this(default(PipelineComponent<TContext>))
        {
            Pipeline = pipeline;
        }

        [DebuggerHidden]
        internal override Task Receive(IPipelineMessage<TContext> message)
        {
            // By this point validation has been done, direct cast
            return _bridge((IPipelineMessage<TData, TContext>)message);

            //////Log("- Receiving {0} on Thread {1}", ((IPipelineMessage<object>)message).Data.GetType().Name, Thread.CurrentThread.ManagedThreadId);
            ////var handledAwait = default(Task);
            ////// async keyword not needed, chain maintained on Task
            ////try
            ////{
            ////    var pipelineMessage = message as IPipelineMessage<TData, TContext>;
            ////    if (pipelineMessage == null)
            ////        throw new Exception("Cast failure in bridge");
            ////    await Bridge(pipelineMessage);
            ////}
            ////catch (Exception exception)
            ////{
            ////    var handled = message.RaiseException(exception);
            ////    if( handled )
            ////        handledAwait = Task.FromResult(exception);
            ////    else
            ////        throw;
            ////}
            ////if (handledAwait != null)
            ////    await handledAwait;
        }

        internal void Calls(Func<IPipelineMessage<TData, TContext>, Task> bridge)
        {
            _bridge = bridge;
        }
    }
}