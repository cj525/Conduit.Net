using System;
using System.Threading.Tasks;
using Pipes.Abstraction;
using Pipes.Exceptions;
using Pipes.Implementation;
using Pipes.Interfaces;
using Pipes.Stubs;

namespace Pipes.Types
{
    internal abstract class MessageTap<TScope> : Conduit<TScope>
    {
        protected bool Blackhole;

        internal abstract void AttachTo(Pipeline<TScope> pipeline);

        protected MessageTap(Stub<TScope> source, Stub<TScope> target, Type messageType) : base(source, target, messageType)
        {
            
        }
    }
    internal class MessageTap<TData, TScope> : MessageTap<TScope>, IDisposable, IPipelineMessageTap<TData,TScope> where TData : class
    {
        private readonly MessageTarget<TData,TScope> _messageTarget;
        private readonly ReceiverStub<TData,TScope> _rxStub;

        internal MessageTap(Pipeline<TScope> pipeline) : base(null, null, typeof(TData))
        {
            // A wee bit dirty, but it's leads to nice code reuse
            // "Target" is the conduit's message receiver
            // _messageTarget is the same bridge which holds the target function's delegate
            //    for both Taps and PipelineComponents
            // _receiver is invokable side of _messageTarget, which has .Receive
            _rxStub = new ReceiverStub<TData,TScope>(pipeline);
            _messageTarget = new MessageTarget<TData,TScope>();
            _messageTarget.Attach(_rxStub);


            Receiver = _rxStub;
            // Therefore, if( Target is ReceiverStub ) during message handling
            // means that you're handling a tap.  Otherwise the target is likely
            // a ConstructorStub.
        }

        internal override void AttachTo(Pipeline<TScope> pipeline)
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

        public IPipelineConnectorAsync WhichUnwrapsAndCalls(Action<TData> target)
        {
            Blackhole = false;
            _messageTarget.Set(target);

            return this;
        }

        public IPipelineConnectorAsync WhichCalls(Action<IPipelineMessage<TData,TScope>> target)
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

        public IPipelineConnectorAsync WhichUnwrapsAndCallsAsync(Func<TData, Task> target)
        {
            Blackhole = false;
            _messageTarget.Set(target);

            return this;
        }

        public IPipelineConnectorAsync WhichCallsAsync(Func<IPipelineMessage<TData, TScope>, Task> target)
        {
            Blackhole = false;
            _messageTarget.Set(target);

            return this;
        }


    }
}
