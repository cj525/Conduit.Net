using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Threading;
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

        private List<Conduit> _manifold;
        private MessageQueueThread _queueThread;
        private MessageQueueThread[] _queueThreads;
        private int _poolPtr;
        private bool _isManifold;


        internal ReceiverStub Receiver;

        internal Stub Source { get; set; }

        internal Stub Target { get; set; }

        internal Type MessageType { get; set; }

        internal bool IsPrivate { get; set; }

        internal bool OffThread { get; set; }
        internal bool WithWait { get; set; }
        
        internal bool Pooled { get; set; }

        internal int QueueLength { get; set; }


        public Conduit(Stub source, Stub target, Type messageType = default(Type))
        {
            Target = target;
            Source = source;
            MessageType = messageType;
            QueueLength = DefaultBufferLength;
            WithWait = true;
        }

        public IPipelineConnectorAsyncBuffered OnSeparateThread()
        {
            OffThread = true;
            return this;
        }

        public IPipelineConnectorAsyncWait InParallel()
        {
            OffThread = true;
            Pooled = true;
            return this;
        }

        public IPipelineConnectorAsyncWait WithQueueLengthOf(int queueLength)
        {
            QueueLength = queueLength;
            return this;
        }

        public void WithoutWaiting()
        {
            WithWait = false;
        }


        internal List<Conduit> AsManifold()
        {
            if (_manifold == null)
            {
                _manifold = new List<Conduit>();
                _isManifold = true;
            }
            return _manifold;
        }

        internal Task Invoke(IPipelineMessage message)
        {
            if (Receiver == null && !_isManifold)
                throw new NotAttachedException("Conduit is not attached.");

            if (!OffThread)
            {
                if (Receiver != null)
                {
                    return Receiver.Receive(message);
                }

                var target = _manifold[NextPtr()];
                return target.Invoke(message);
            }
            else
            {
                if (!Pooled)
                {
                    if (_queueThread == null)
                        _queueThread = new MessageQueueThread(Target.ContainedType.Name);

                    _queueThread.Enqueue(WithWait ? () => Receiver.Receive(message).Wait() : (Action) (() => Receiver.Receive(message)));

                    if (!_queueThread.IsStarted)
                    {
                        _queueThread.MaxQueueLength = QueueLength;
                        _queueThread.Start();
                    }
                }
                else
                {
                    if (!_isManifold)
                    {
                        ThreadPool.QueueUserWorkItem(state => Receiver.Receive(message));
                    }
                    else
                    {
                        var count = _manifold.Count;
                        if (_queueThreads == null)
                            lock (_manifold)
                            {
                                if (_queueThreads == null)
                                {
                                    _queueThreads = Enumerable.Range(0, count).Select(index => new MessageQueueThread(String.Format("{0} {1}/{2}", Target.ContainedType.Name, index, count))).ToArray();
                                }

                            }

                        lock (_queueThreads)
                        {
                            var ptr = NextPtr();
                            var thread = _queueThreads[ptr];
                            var target = _manifold[ptr];
                            
                            thread.Enqueue(() => target.Invoke(message));
                        }
                    }
                }
            }
            return Abstraction.Target.EmptyTask;
        }

        private void EnqueueOnManifold(IPipelineMessage message)
        {
            var target = _manifold[_poolPtr];
            
        }

        private int NextPtr()
        {
            lock (_manifold)
            {
                var result = _poolPtr;
                if (++_poolPtr == _manifold.Count)
                    _poolPtr = 0;
                return result;
            }
        }

        internal Conduit Clone(ReceiverStub rx)
        {
            return new Conduit(Source, Target, MessageType)
            {
                IsPrivate = IsPrivate,
                MessageType = MessageType,
                OffThread = OffThread,
                Pooled = Pooled,
                QueueLength = QueueLength,
                Receiver = rx,
                WithWait = WithWait,
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
