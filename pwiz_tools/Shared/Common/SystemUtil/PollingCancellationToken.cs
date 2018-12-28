using System;
using System.Threading;

namespace pwiz.Common.SystemUtil
{
    public class PollingCancellationToken : IDisposable
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly Func<bool> _checkCancelled;
        private readonly Thread _pollingThread;
        private Exception _exception;

        public PollingCancellationToken(Func<bool> checkCancelled, int frequency = 10)
        {
            PollingFrequency = frequency;
            _checkCancelled = checkCancelled;
            _pollingThread = new Thread(PollingThreadMethod);
            _pollingThread.Name = @"PollingCancellationToken";
            _pollingThread.Start();
        }

        public int PollingFrequency { get; set; }
        public CancellationToken Token
        {
            get { return _cancellationTokenSource.Token; }
        }

        public void Dispose()
        {
            lock (this)
            {
                _cancellationTokenSource.Cancel();
                Monitor.Pulse(this);
            }

            _pollingThread.Join();
            if (_exception != null)
            {
                throw new AggregateException(@"Exception on polling thread", _exception);
            }
        }

        private void PollingThreadMethod()
        {
            try
            {
                while (true)
                {
                    if (_cancellationTokenSource.IsCancellationRequested)
                    {
                        return;
                    }

                    if (_checkCancelled())
                    {
                        _cancellationTokenSource.Cancel();
                    }

                    lock (this)
                    {
                        if (_cancellationTokenSource.IsCancellationRequested)
                        {
                            return;
                        }

                        Monitor.Wait(this, PollingFrequency);
                    }
                }
            }
            catch (Exception e)
            {
                _exception = e;
            }
        }

        public static PollingCancellationToken FromProgressMonitor(IProgressMonitor progressMonitor)
        {
            return new PollingCancellationToken(()=>progressMonitor.IsCanceled);
        }
    }
}
