using System;
using System.Threading.Tasks;
using Pipes.Abstraction;
using Pipes.Exceptions;
using Pipes.Implementation;
using Pipes.Interfaces;
using Pipes.Stubs;

namespace Pipes.Types
{
    internal abstract class MessageTap : Conduit
    {
        protected bool Blackhole;

        internal abstract void AttachTo(Pipeline pipeline);

        protected MessageTap(Stub source, Stub target, Type messageType) : base(source,target,messageType)
        {
            
        }
    }
    internal class MessageTap<T> : MessageTap, IDisposable, IPipelineMessageTap<T> where T : class
    {
        private readonly MessageTarget<T> _messageTarget;
        private readonly ReceiverStub<T> _rxStub;

        internal MessageTap(Pipeline pipeline) : base(null, null, typeof(T))
        {
            // A wee bit dirty, but it's leads to nice code reuse
            // "Target" is the conduit's message receiver
            // _messageTarget is the same bridge which holds the target function's delegate
            //    for both Taps and PipelineComponents
            // _receiver is invokable side of _messageTarget, which has .Receive
            _rxStub = new ReceiverStub<T>(pipeline);
            _messageTarget = new MessageTarget<T>();
            _messageTarget.Attach(_rxStub);


            Receiver = _rxStub;
            // Therefore, if( Target is ReceiverStub ) during message handling
            // means that you're handling a tap.  Otherwise the target is likely
            // a ConstructorStub.
        }

        internal override void AttachTo(Pipeline pipeline)
        {
            if (Blackhole)
                throw new NotAttachedException("MessageTap is black-hole. (rx without delegate)");

            _messageTarget.Attach(_rxStub);
            pipeline.AddRx(_rxStub, null);
        }

        
        public IPipelineConnectorAsync WhichTriggers(Action target)
        {
            Blackhole = false;
            _messageTarget.Set(target);

            return this;
        }

        public IPipelineConnectorAsync WhichUnwrapsAndCalls(Action<T> target)
        {
            Blackhole = false;
            _messageTarget.Set(target);

            return this;
        }

        public IPipelineConnectorAsync WhichCalls(Action<IPipelineMessage<T>> target)
        {
            Blackhole = false;
            _messageTarget.Set(target);
            
            return this;
        }

        public IPipelineConnectorAsync WhichTriggersAsync(Func<Task> target)
        {
            Blackhole = false;
            _messageTarget.Set(target);

            return this;
        }

        public IPipelineConnectorAsync WhichUnwrapsAndCallsAsync(Func<T, Task> target)
        {
            Blackhole = false;
            _messageTarget.Set(target);

            return this;
        }

        public IPipelineConnectorAsync WhichCallsAsync(Func<IPipelineMessage<T>, Task> target)
        {
            Blackhole = false;
            _messageTarget.Set(target);

            return this;
        }


    }
}
