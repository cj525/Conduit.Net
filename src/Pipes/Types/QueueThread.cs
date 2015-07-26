using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Pipes.Extensions;

namespace Pipes.Types
{
    internal class QueueThread
    {
        private readonly Thread _thread;
        private readonly object _bufferMutex = new { };
        private readonly ManualResetEventSlim _doneEvent;
        private readonly ManualResetEventSlim _fullEvent;
        private readonly ManualResetEventSlim _waitForWorkEvent;
        private readonly CancellationTokenSource _semaphoreCancelSource;
        private readonly CancellationToken _semaphoreCancelToken;

        private bool _quit;
        private Queue<Action> _workQueue;
        private Queue<Action> _backBuffer;

        public bool WasStarted { get; private set; }
        public bool HasQuit { get; private set; }
        public bool WasCancelled { get; private set; }

        /// <summary>
        /// Sets or gets the maximum number of items that can be queued.  
        /// Theoretically the maximum numbers of items "in flight" is twice this number.
        /// </summary>
        public int MaxQueueLength { get; set; }

        public QueueThread( string name )
        {
            _thread = new Thread(ThreadLoop) {IsBackground = true, Name = "Message Queue - " + name};

            _fullEvent = new ManualResetEventSlim( true );              // Not full at startup
            _waitForWorkEvent = new ManualResetEventSlim( false );      // Waiting for work at startup
            _doneEvent = new ManualResetEventSlim( false );             // Waiting for done at startup
            _semaphoreCancelSource = new CancellationTokenSource();     // Cancel source for waits
            _semaphoreCancelToken = _semaphoreCancelSource.Token;       // Slightly faster access to token

            _workQueue = new Queue<Action>();                       // Buffer to be worked
            _backBuffer = new Queue<Action>();                      // Buffer to enqueue

        }

        public void Start()
        {
            if( WasStarted )
                throw new NotImplementedException("Can't restart a thread.");

            WasStarted = true;
            _quit = false;

            _thread.Start();
        }

        public void Enqueue(Action action)
        {
            // Checking count requires a lock
            lock (_bufferMutex)
            {
                // If the queue is full cause a wait
                if( MaxQueueLength > 0 && _backBuffer.Count >= MaxQueueLength )
                    _fullEvent.Reset();
            }

            // Wait if full
            _fullEvent.Wait( _semaphoreCancelToken );

            // If wait was cancelled, explode here
            if( _semaphoreCancelSource.IsCancellationRequested )
                throw new OperationCanceledException( "Message Queue was cancelled during blocked enqueue." );


            lock( _bufferMutex )
            {
                // If cancel happened here, the thread loop will exit
                _backBuffer.Enqueue( action );

                // Release a pending wait
                _waitForWorkEvent.Set();
            }

        }


        /// <summary>
        /// Causes the thread to quit.
        /// Pending operations will throw an OperationCanceledException.
        /// </summary>
        public void Stop()
        {
            lock (_bufferMutex)
            {
                // Set quit signals
                _quit = true;
                _semaphoreCancelSource.Cancel();

                // Disengage lock
                _waitForWorkEvent.Set();

            }

            _doneEvent.Wait( _semaphoreCancelToken );
        }

        /// <summary>
        /// Waits for work to stop and allows the thread to exit cleanly.
        /// If addition items are enqueued while waiting, a race condition 
        /// will ensue which may cause an OperationCanceled exception.
        /// </summary>
        public void WaitForEmpty()
        {
            // If the thread never started, exit cleanly.
            if (!WasStarted)
                return;
            
            // Wait for work to stop
            while (!_quit)
            {
                lock (_bufferMutex)
                {
                    // Check for a zero count (total)
                    if (_workQueue.Count + _backBuffer.Count == 0)
                    {
                        // Exit thread loop
                        _quit = true;

                        // Disengage lock
                        _waitForWorkEvent.Set();
                    }
                }
                
                // Yield to worker
                Thread.Yield();
            }

            // Wait for thread loop to exit
            _doneEvent.Wait(_semaphoreCancelToken);
        }

        private void ThreadLoop()
        {
            while (!_quit)
            {
                // Locals
                Action[] workList = null;

                // Wait for work
                _waitForWorkEvent.Wait( _semaphoreCancelToken );

                // Was cancelled?  Exit cleanly
                if( _semaphoreCancelToken.IsCancellationRequested )
                    break;

                // Do not swap the queues while adding the two counts together
                lock( _bufferMutex )
                {
                    // Swap the back buffer
                    _workQueue = Interlocked.Exchange( ref _backBuffer, _workQueue );

                    // Copy work queue out
                    workList = _workQueue.ToArray();

                    // Clear the queue
                    _workQueue.Clear();

                    // Wait for work at next loop
                    _waitForWorkEvent.Reset();

                    // If we're waiting for space, release
                    _fullEvent.Set();
                }

                // Work the queue provide quit check
                foreach( var work in workList )
                {
                    // Bail?  Here we bail cleanly (no place to land an exception)
                    if( _semaphoreCancelToken.IsCancellationRequested )
                        break;

                    // Perform the work
                    work();
                }
            }

            // Signal shutdown to close
            _doneEvent.Set();

            // Convey status publicly
            HasQuit = true;
            WasCancelled = !_quit;
        }
    }
}
