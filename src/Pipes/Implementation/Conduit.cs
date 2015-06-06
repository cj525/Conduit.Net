using System;
using System.Threading.Tasks;
using Pipes.Abstraction;
using Pipes.Exceptions;
using Pipes.Interfaces;
using Pipes.Stubs;

namespace Pipes.Implementation
{
    internal class Conduit : IPipelineConnectorAsync, IPipelineConnectorAsyncBuffered
    {

        public static int DefaultBufferLength = 1000;

        private MessageQueueThread _queueThread;


        internal ReceiverStub Receiver;

        internal Stub Source { get; set; }

        internal Stub Target { get; set; }

        internal Type MessageType { get; set; }

        internal bool IsPrivate { get; set; }

        internal bool OffThread { get; set; }
        
        internal bool Pooled { get; set; }

        internal int QueueLength { get; set; }


        public Conduit(Stub source, Stub target, Type messageType = default(Type))
        {
            Target = target;
            Source = source;
            MessageType = messageType;
            QueueLength = DefaultBufferLength;
        }

        public IPipelineConnectorAsyncBuffered OnSeparateThread()
        {
            OffThread = true;
            return this;
        }

        public void InParallel()
        {
            OffThread = true;
            Pooled = true;
        }

        public void WithQueueLengthOf(int queueLength)
        {
            QueueLength = queueLength;
        }

        internal async Task Invoke(IPipelineMessage message)
        {
            if( Receiver == null )
                throw new NotAttachedException("Conduit is not attached.");

            if (!OffThread)
            {
                await Receiver.Receive(message);
            }
            else
            {
                if (!Pooled)
                {
                    if( _queueThread == null )
                        _queueThread = new MessageQueueThread(Target.ContainedType.Name);

                    _queueThread.Enqueue(() => Receiver.Receive(message).Wait());

                    if (!_queueThread.IsStarted)
                    {
                        _queueThread.MaxQueueLength = QueueLength;
                        _queueThread.Start();
                    }
                }
                else
                {
                    await Task.Run(() => Receiver.Receive(message).Wait());
                }
            }
        }

        internal Conduit Procreate(ReceiverStub rx)
        {
            return new Conduit(Source, Target, MessageType)
            {
                IsPrivate = IsPrivate,
                MessageType = MessageType,
                OffThread = OffThread,
                Pooled = Pooled,
                QueueLength = QueueLength,
                Receiver = rx
            };
        }

        internal void Shutdown()
        {
            if( _queueThread != null )
                _queueThread.Shutdown();
        }

        public void Dispose()
        {
            _queueThread.Stop();
        }

        internal class Partial<T> : Conduit, IPipelineMessageSingleTarget<T> where T : class
        {
            public Partial(Stub source) : base(source, null, typeof(T))
            {
            }

            public IPipelineConnectorAsync To(Stub target)
            {
                Target = target;

                return this;
            }
        }
    }
}
