using System;
using System.Threading.Tasks;
using Pipes.Abstraction;
using Pipes.Exceptions;
using Pipes.Implementation;
using Pipes.Interfaces;
using Pipes.Stubs;

namespace Pipes.BuildingBlocks
{
    internal class Receiver<T> : BuildingBlock, IPipelineMessageReceiver<T> where T : class
    {
        private readonly ReceiverStub<T> _receiver;
        private readonly MessageTarget<T> _messageTarget;

        private bool _blackhole = true;

        public Receiver(PipelineComponent component) : base(component)
        {
            _receiver = new ReceiverStub<T>(component);
            _messageTarget = new MessageTarget<T>();
        }

        protected override void AttachPipeline(Pipeline pipeline)
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

        public void WhichUnwrapsAndCalls(Action<T> action)
        {
            _blackhole = false;
            _messageTarget.Set(action);
        }

        public void WhichCalls(Action<IPipelineMessage<T>> action)
        {
            _blackhole = false;
            _messageTarget.Set(action);
        }

        public void WhichTriggersAsync(Func<Task> asyncAction)
        {
            _blackhole = false;
            _messageTarget.Set(asyncAction);
        }

        public void WhichUnwrapsAndCallsAsync(Func<T, Task> asyncAction)
        {
            _blackhole = false;
            _messageTarget.Set(asyncAction);
        }

        public void WhichCallsAsync(Func<IPipelineMessage<T>, Task> asyncAction)
        {
            _blackhole = false;
            _messageTarget.Set(asyncAction);
        }
    }
}