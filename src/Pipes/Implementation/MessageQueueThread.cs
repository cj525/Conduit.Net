using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Pipes.Implementation
{
    internal class MessageQueueThread
    {
        private readonly object _lockObject = new { };
        private readonly AutoResetEvent _signal;
        private readonly ManualResetEventSlim _reverseSignal;
        private readonly ManualResetEventSlim _doneSignal;
        private readonly Thread _thread;

        private volatile bool _quit;

        private Queue<Action> _workQueue;
        private Queue<Action> _backBuffer;

        public bool IsStarted { get; private set; }

        public int MaxQueueLength { get; set; }

        public MessageQueueThread( string name )
        {
            _thread = new Thread(ThreadLoop) {IsBackground = true, Name = "Message Queue - " + name};
            _signal = new AutoResetEvent(false);
            _reverseSignal = new ManualResetEventSlim(false);
            _doneSignal = new ManualResetEventSlim(false);
            _workQueue = new Queue<Action>();
            _backBuffer = new Queue<Action>();
        }

        public void Start()
        {
            if( IsStarted )
                throw new NotImplementedException("Can't restart a thread.");

            IsStarted = true;
            _quit = false;

            _signal.Reset();
            _reverseSignal.Reset();

            _thread.Start();
        }

        public void Stop()
        {
            lock (_lockObject)
            {
                _quit = true;
                _signal.Set();
            }

            _doneSignal.Wait();
        }

        public void Enqueue(Action action)
        {
            var queueFull = false;

            lock (_lockObject)
            {
                queueFull = MaxQueueLength > 0 && _backBuffer.Count >= MaxQueueLength;
            }

            if (!_quit && queueFull)
            {
                _reverseSignal.Wait();
                _reverseSignal.Reset();
            }

            lock (_lockObject)
            {
                if( !_quit )
                    _backBuffer.Enqueue(action);
            }

            _signal.Set();
        }

        private void ThreadLoop()
        {
            while (!_quit)
            {
                lock (_lockObject)
                {
                    _workQueue = Interlocked.Exchange(ref _backBuffer, _workQueue);
                }

                while (_workQueue.Any() && !_quit)
                {
                    _workQueue.Dequeue()();
                }

                _reverseSignal.Set();
                _signal.WaitOne();
            }
            _doneSignal.Set();
        }

        internal void Shutdown()
        {
            if (!IsStarted)
                return;
            
            while (!_quit)
            {
                lock (_lockObject)
                {
                    if (_workQueue.Count + _backBuffer.Count == 0)
                    {
                        _quit = true;
                        _signal.Set();
                    }
                }
                
                Thread.Yield();
            }
            _doneSignal.Wait();
        }
    }
}
