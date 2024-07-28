using System;
using System.Collections.Generic;
using System.Threading;

namespace pwiz.Common.SystemUtil
{
    public class ActionQueue : IDisposable
    {
        private CancellationTokenSource _cancellationTokenSource;
        private QueueWorker<Action> _actionQueue;
        private List<Exception> _exceptions;
        private int _pendingCount;
        private long _currentIndex;
        private long _lastIndex;

        public void RunAsync(int threadCount, string name)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _actionQueue = new QueueWorker<Action>(null, RunAction);
            _actionQueue.RunAsync(threadCount, name);
        }

        public CancellationToken CancellationToken
        {
            get
            {
                return _cancellationTokenSource.Token;
            }
        }

        public void Enqueue(Action action, string description)
        {
            lock (this)
            {
                CheckForExceptions();
                long index = Interlocked.Increment(ref _currentIndex);
                Console.Out.WriteLine("Thread: {0} Enqueue {1}: {2}", Thread.CurrentThread.ManagedThreadId, index, description);
                _actionQueue.Add(() =>
                {
                    if (_lastIndex != index - 1)
                    {
                        throw new InvalidOperationException(string.Format("Expected {0} actual {1}", index - 1,
                            _lastIndex));
                    }

                    _lastIndex = index;
                    Console.Out.WriteLine("Execute {0}, {1}", index, description);
                    action();
                });
                // _actionQueue.Add(action);
                _pendingCount++;
            }
        }

        private void RunAction(Action action, int threadId)
        {
            try
            {
                if (!AnyExceptions())
                {
                    action();
                }
            }
            catch (Exception exception)
            {
                OnException(exception);
            }
            finally
            {
                lock (this)
                {
                    _pendingCount--;
                    if (_pendingCount == 0)
                    {
                        Monitor.PulseAll(this);
                    }
                }
                
            }
        }

        public bool AnyExceptions()
        {
            lock (this)
            {
                return _exceptions != null;
            }
        }

        public void CheckForExceptions()
        {
            lock (this)
            {
                if (_exceptions != null)
                {
                    throw new AggregateException(_exceptions);
                }
            }
        }

        public void OnException(Exception exception)
        {
            lock (this)
            {
                bool firstException = _exceptions == null;
                _exceptions ??= new List<Exception>();
                _exceptions.Add(exception);
                if (firstException)
                {
                    _cancellationTokenSource.Cancel();
                    Monitor.PulseAll(this);
                }
            }
        }

        public void WaitForComplete()
        {
            lock (this)
            {
                while (_pendingCount > 0)
                {
                    CheckForExceptions();
                    Monitor.Wait(this);
                }
            }
        }

        public void Dispose()
        {
            _actionQueue.Dispose();
        }
    }
}
