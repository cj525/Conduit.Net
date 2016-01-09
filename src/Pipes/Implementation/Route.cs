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
using Pipes.Types;

namespace Pipes.Implementation
{
    internal class Route<TContext> : IPipelineConnectorAsync, IPipelineConnectorInParallel, IPipelineConnectorOnSeparateThread where TContext : OperationContext
    {

        //public static int DefaultBufferLength = 1000;
        //private readonly object _lockObject = new {};
        //private QueueThread _queueThread;
        //private QueueThread[] _queueThreads;
        //private int _poolPtr;

        internal ReceiverStub<TContext> Receiver;

        internal Stub<TContext> Source { get; set; }

        internal Stub<TContext> Target { get; set; }

        internal Type DataType { get; set; }

        internal bool IsPrivate { get; set; }

        internal bool OffThread { get; set; }

        internal bool WithWait { get; set; }
        
        internal bool Parallel { get; set; }

        internal int QueueLength { get; set; }

        internal bool NeedsCompletion { get; set; }
        internal int MaxConcurrency { get; set; }


        public Route()
        {
            
        }
        public Route(Stub<TContext> source, Stub<TContext> target, Type dataType = default(Type))
        {
            Target = target;
            Source = source;
            DataType = dataType;
            //QueueLength = DefaultBufferLength;
            WithWait = true;
        }

        void IPipelineConnectorInParallel.OnSeparateThread()
        {
            OffThread = true;
        }

        void IPipelineConnectorOnSeparateThread.InParallel()
        {
            Parallel = true;
        }

        public IPipelineConnectorOnSeparateThread OnSeparateThread()
        {
            OffThread = true;
            return this;
        }

        public IPipelineConnectorInParallel InParallel()
        {
            Parallel = true;
            return this;
        }

        //public IPipelineConnector WithCompletion(int maxConcurrency = 0)
        //{
        //    MaxConcurrency = maxConcurrency;
        //    NeedsCompletion = true;
        //    return this;
        //}


        //[DebuggerHidden]
        //internal Task Invoke(IPipelineMessage<TContext> message)
        //{
        //    if (!OffThread)
        //    {
        //        if (Receiver != null)
        //        {
        //            return Receiver.Receive(message);
        //        }

        //        if (_manifold != null)
        //        {
        //            var target = _manifold[NextPtr()];
        //            return target.Invoke(message);
        //        }

        //        if (Receiver == null && _manifold == null)
        //            throw new NotAttachedException("Conduit is not attached.");
        //    }
        //    else if (!Pooled)
        //    {
        //        if (_queueThread == null)
        //        {
        //            lock (_lockObject)
        //            {
        //                if (_queueThread == null)
        //                    _queueThread = new QueueThread(Target.ContainedType.Name);
        //            }
        //        }
        //        // == true for mono bug
        //        _queueThread.Enqueue(() => Receiver.Receive(message));

        //        // Double-check pattern does work, check Lazy<T> if you don't believe me
        //        if (!_queueThread.WasStarted)
        //        {
        //            lock (_lockObject)
        //            {
        //                if (!_queueThread.WasStarted)
        //                {
        //                    _queueThread.MaxQueueLength = QueueLength;
        //                    _queueThread.Start();
        //                }
        //            }
        //        }
        //    }
        //    else
        //    {
        //        if (_manifold == null)
        //        {
        //            ThreadPool.QueueUserWorkItem(state => Receiver.Receive(message).Wait());
        //        }
        //        else
        //        {
        //            var count = _manifold.Count;
        //            // Double-check pattern does work, check Lazy<T> if you don't believe me
        //            if( _queueThreads == null )
        //            {
        //                lock( _manifold )
        //                {
        //                    if( _queueThreads == null )
        //                    {
        //                        _queueThreads = Enumerable.Range( 0, count ).Select( index => new QueueThread( String.Format( "{0} {1}/{2}", Target.ContainedType.Name, index, count ) ) ).ToArray();
        //                    }
        //                }
        //            }

        //            lock (_queueThreads)
        //            {
        //                var ptr = NextPtr();
        //                var thread = _queueThreads[ptr];
        //                var target = _manifold[ptr];
                            
        //                thread.Enqueue(() => target.Invoke(message));
        //            }
        //        }
        //    }

        //    return Abstraction.Target.EmptyTask;
        //}

        //private int NextPtr()
        //{
        //    lock (_manifold)
        //    {
        //        var result = _poolPtr;
        //        if (++_poolPtr == _manifold.Count)
        //            _poolPtr = 0;
        //        return result;
        //    }
        //}

        //internal Route<TContext> Clone(ReceiverStub<TContext> rx)
        //{
        //    return new Route<TContext>(Source, Target, DataType)
        //    {
        //        IsPrivate = IsPrivate,
        //        DataType = DataType,
        //        OffThread = OffThread,
        //        Pooled = Pooled,
        //        QueueLength = QueueLength,
        //        Receiver = rx,
        //        WithWait = WithWait,
        //    };
        //}

        //internal void ShutdownThreads()
        //{
        //    _queueThread?.WaitForEmpty();

        //    _queueThread = null;
        //}

        //public void Dispose()
        //{
        //    ShutdownThreads();
        //}

        internal class Partial<T> : Route<TContext>, IPipelineMessageSingleTarget<TContext> where T : class
        {
            public Partial(Stub<TContext> source) : base(source, null, typeof(T))
            {
            }

            public void To(Stub<TContext> target)
            {
                Target = target;
            }
        }

    }
}
