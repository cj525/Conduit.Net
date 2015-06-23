using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Pipes.Abstraction;
using Pipes.Exceptions;
using Pipes.Interfaces;
using Pipes.Stubs;

namespace Pipes.Implementation
{
    internal class Conduit<TScope> : IPipelineConnectorAsync, IPipelineConnectorAsyncBuffered
    {

        public static int DefaultBufferLength = 1000;

        private List<Conduit<TScope>> _manifold;
        private MessageQueueThread _queueThread;
        private MessageQueueThread[] _queueThreads;
        private int _poolPtr;
        private bool _isManifold;


        internal ReceiverStub<TScope> Receiver;

        internal Stub<TScope> Source { get; set; }

        internal Stub<TScope> Target { get; set; }

        internal Type MessageType { get; set; }

        internal bool IsPrivate { get; set; }

        internal bool OffThread { get; set; }
        internal bool WithWait { get; set; }
        
        internal bool Pooled { get; set; }

        internal int QueueLength { get; set; }


        public Conduit(Stub<TScope> source, Stub<TScope> target, Type messageType = default(Type))
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


        internal List<Conduit<TScope>> AsManifold()
        {
            if (_manifold == null)
            {
                _manifold = new List<Conduit<TScope>>();
                _isManifold = true;
            }
            return _manifold;
        }

        [DebuggerHidden]
        internal async Task Invoke(IPipelineMessage<TScope> message)
        {
            if (Receiver == null && !_isManifold)
                throw new NotAttachedException("Conduit is not attached.");

            if (!OffThread)
            {
                if (Receiver != null)
                {
                    await Receiver.Receive(message);
                }
                else
                {
                    var target = _manifold[NextPtr()];
                    await target.Invoke(message);
                }
            }
            else
            {
                if (!Pooled)
                {
                    if (_queueThread == null)
                        _queueThread = new MessageQueueThread(Target.ContainedType.Name);

                    // == true for mono bug
                    _queueThread.Enqueue(WithWait == true? () => Receiver.Receive(message).Wait() : (Action) (() => Receiver.Receive(message)));

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
            await Abstraction.Target.EmptyTask;
        }

        private void EnqueueOnManifold(IPipelineMessage<TScope> message)
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

        internal Conduit<TScope> Clone(ReceiverStub<TScope> rx)
        {
            return new Conduit<TScope>(Source, Target, MessageType)
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

        internal class Partial<T> : Conduit<TScope>, IPipelineMessageSingleTarget<TScope> where T : class
        {
            public Partial(Stub<TScope> source) : base(source, null, typeof(T))
            {
            }

            public IPipelineConnectorAsync To(Stub<TScope> target)
            {
                Target = target;

                return this;
            }
        }

    }
}
