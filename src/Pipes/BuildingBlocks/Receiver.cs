using System;
using System.Threading.Tasks;
using Pipes.Abstraction;
using Pipes.Exceptions;
using Pipes.Implementation;
using Pipes.Interfaces;
using Pipes.Stubs;

namespace Pipes.BuildingBlocks
{
    internal class Receiver<TData, TContext> : BuildingBlock<TContext>, IPipelineMessageReceiver<TData, TContext>
        where TData : class
        where TContext : class
    {
        private readonly ReceiverStub<TData,TContext> _receiver;
        private readonly MessageTarget<TData,TContext> _messageTarget;

        private bool _blackhole = true;

        public Receiver(IPipelineComponent<TContext> component) : base(component)
        {
            _receiver = new ReceiverStub<TData,TContext>(component);
            _messageTarget = new MessageTarget<TData,TContext>();
        }

        protected override void AttachPipeline(Pipeline<TContext> pipeline)
        {
            if (_blackhole)
                throw new NotAttachedException("Receiver is black-hole. (rx without delegate)");

            if( Component == null )
                throw new NotImplementedException("Casted the builder?");

            // Create rx socket
            _messageTarget.Attach(_receiver);
            pipeline.AddRx(_receiver,Component);
        }

        public void WhichTriggers(Action action)
        {
            _blackhole = false;
            _messageTarget.Set(action);
        }

        public void WhichUnwrapsAndCalls(Action<TData> action)
        {
            _blackhole = false;
            _messageTarget.Set(action);
        }

        public void WhichCalls(Action<IPipelineMessage<TData,TContext>> action)
        {
            _blackhole = false;
            _messageTarget.Set(action);
        }

        public void WhichTriggersAsync(Func<Task> asyncAction)
        {
            _blackhole = false;
            _messageTarget.Set(asyncAction);
        }

        public void WhichUnwrapsAndCallsAsync(Func<TData, Task> asyncAction)
        {
            _blackhole = false;
            _messageTarget.Set(asyncAction);
        }

        public void WhichCallsAsync(Func<IPipelineMessage<TData,TContext>, Task> asyncAction)
        {
            _blackhole = false;
            _messageTarget.Set(asyncAction);
        }
    }
}