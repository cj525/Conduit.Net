using System;
using System.Threading.Tasks;
using Pipes.Abstraction;
using Pipes.Exceptions;
using Pipes.Implementation;
using Pipes.Interfaces;
using Pipes.Stubs;

namespace Pipes.BuildingBlocks
{
    internal class Receiver<TData,TScope> : BuildingBlock<TScope>, IPipelineMessageReceiver<TData,TScope> where TData : class
    {
        private readonly ReceiverStub<TData,TScope> _receiver;
        private readonly MessageTarget<TData,TScope> _messageTarget;

        private bool _blackhole = true;

        public Receiver(IPipelineComponent<TScope> component) : base(component)
        {
            _receiver = new ReceiverStub<TData,TScope>(component);
            _messageTarget = new MessageTarget<TData,TScope>();
        }

        protected override void AttachPipeline(Pipeline<TScope> pipeline)
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

        public void WhichCalls(Action<IPipelineMessage<TData,TScope>> action)
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

        public void WhichCallsAsync(Func<IPipelineMessage<TData,TScope>, Task> asyncAction)
        {
            _blackhole = false;
            _messageTarget.Set(asyncAction);
        }
    }
}