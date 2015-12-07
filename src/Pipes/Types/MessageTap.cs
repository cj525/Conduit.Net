using System;
using System.Threading.Tasks;
using Pipes.Abstraction;
using Pipes.Exceptions;
using Pipes.Implementation;
using Pipes.Interfaces;
using Pipes.Stubs;

namespace Pipes.Types
{
    internal abstract class MessageTap<TContext> : Conduit<TContext>
        where TContext : class, IOperationContext
    {
        protected bool Blackhole;

        internal abstract void AttachTo(Pipeline<TContext> pipeline);

        protected MessageTap(Stub<TContext> source, Stub<TContext> target, Type messageType) : base(source, target, messageType)
        {
            
        }
    }

    internal class MessageTap<TData, TContext> : MessageTap<TContext>, IDisposable, IPipelineMessageTap<TData,TContext> 
        where TData : class
        where TContext : class, IOperationContext
    {
        private readonly MessageTarget<TData,TContext> _messageTarget;
        private readonly ReceiverStub<TData,TContext> _rxStub;

        internal MessageTap(Pipeline<TContext> pipeline) : base(null, null, typeof(TData))
        {
            // A wee bit dirty, but it's leads to nice code reuse
            // "Target" is the conduit's message receiver
            // _messageTarget is the same bridge which holds the target function's delegate
            //    for both Taps and PipelineComponents
            // _receiver is invokable side of _messageTarget, which has .Receive
            _rxStub = new ReceiverStub<TData,TContext>(pipeline);
            _messageTarget = new MessageTarget<TData,TContext>();
            _messageTarget.Attach(_rxStub);


            Receiver = _rxStub;
            // Therefore, if( Target is ReceiverStub ) during message handling
            // means that you're handling a tap.  Otherwise the target is likely
            // a ConstructorStub.
        }

        internal override void AttachTo(Pipeline<TContext> pipeline)
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

        public IPipelineConnectorAsync WhichCalls(Action<IPipelineMessage<TData,TContext>> target)
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

        public IPipelineConnectorAsync WhichCallsAsync(Func<IPipelineMessage<TData, TContext>, Task> target)
        {
            Blackhole = false;
            _messageTarget.Set(target);

            return this;
        }


    }
}
