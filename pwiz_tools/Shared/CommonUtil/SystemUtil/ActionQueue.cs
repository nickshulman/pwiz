﻿using System;
using System.Collections.Generic;
using System.Threading;

namespace pwiz.Common.SystemUtil
{
    public class ActionQueue : IDisposable
    {
        private QueueWorker<Action> _actionQueue;
        private List<Exception> _exceptions;
        private int _pendingCount;

        public void RunAsync(int threadCount, string name)
        {
            _actionQueue = new QueueWorker<Action>(null, RunAction);
            _actionQueue.RunAsync(threadCount, name);
        }

        public void Enqueue(Action action)
        {
            lock (this)
            {
                CheckForExceptions();
                _actionQueue.Add(action);
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
