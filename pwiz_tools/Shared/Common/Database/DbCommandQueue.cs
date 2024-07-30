using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using pwiz.Common.SystemUtil;

namespace pwiz.Common.Database
{
    public class DbCommandQueue : IDisposable
    {
        private QueueWorker<IDbCommand> _queueWorker;
        private List<Exception> _exceptions;
        private int _pendingCount;
        public DbCommandQueue()
        {
            _queueWorker = new QueueWorker<IDbCommand>(null, ExecuteCommand);
        }

        public void Dispose()
        {
            _queueWorker?.Dispose();
        }

        public void Flush()
        {
            lock (this)
            {
                while (_pendingCount > 0)
                {
                    if (_exceptions.Any())
                    {
                        return;
                    }

                    Monitor.Wait(this);
                }
            }
        }

        public void QueueCommand(IDbCommand command)
        {
            CheckForExceptions();
            lock (this)
            {
                if (_queueWorker == null)
                {
                    _queueWorker = new QueueWorker<IDbCommand>(null, ExecuteCommand);
                    _queueWorker.RunAsync(1, nameof(ExecuteCommand));
                }
                _queueWorker.Add(command);
                _pendingCount++;
            }
        }

        private void ExecuteCommand(IDbCommand command, int threadIndex)
        {
            try
            {
                lock (_exceptions)
                {
                    if (_exceptions.Any())
                    {
                        return;
                    }
                }
                command.Execute();
            }
            catch (Exception ex)
            {
                lock (this)
                {
                    _exceptions.Add(ex);
                }
            }
            finally
            {
                lock (this)
                {
                    Interlocked.Decrement(ref _pendingCount);
                    if (_pendingCount == 0)
                    {
                        Monitor.PulseAll(this);
                    }
                }
            }
        }

        private void CheckForExceptions()
        {
            lock (this)
            {
                if (_exceptions.Any())
                {
                    throw new AggregateException(_exceptions);
                }
            }
        }
    }

    public interface IDbCommand
    {
        void Execute();
    }
}
