using System;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace CrossLinkerTool
{
    public partial class LongWaitDlg : Form
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private Exception _exception;
        private readonly object _lock = new object();
        private bool _finished;
        private bool _windowShown;
        public LongWaitDlg()
        {
            InitializeComponent();
        }

        public void PerformWork(Control parent, int delayMillis, Action performWork)
        {
            try
            {
                Action runner = () =>
                {
                    try
                    {
                        performWork();
                    }
                    catch (Exception e)
                    {
                        _exception = e;
                    }
                    finally
                    {
                        lock (_lock)
                        {
                            _finished = true;
                            if (_windowShown)
                            {
                                BeginInvoke(new Action(Close));
                            }
                        }
                    }
                };
                var asyncResult = runner.BeginInvoke(runner.EndInvoke, null);

                // Wait as long as the caller wants before showing the progress
                // animation to the user.
                asyncResult.AsyncWaitHandle.WaitOne(delayMillis);

                // Return without notifying the user, if the operation completed
                // before the wait expired.
                if (asyncResult.IsCompleted)
                    return;

                progressBar.Value = Math.Max(0, ProgressValue);
                labelMessage.Text = Message;
                ShowDialog(parent);
            }
            finally
            {
                var exception = _exception;
                // Get rid of this window before leaving this function
                Dispose();

                if (CancellationToken.IsCancellationRequested && null != exception)
                {
                    if (exception is OperationCanceledException || exception.InnerException is OperationCanceledException)
                    {
                        exception = null;
                    }
                }

                if (exception != null)
                {
                    throw new TargetInvocationException(exception.Message, exception);
                }
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            lock (_lock)
            {
                if (_finished)
                {
                    Close();
                }
                else
                {
                    _windowShown = true;
                }
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            lock (_lock)
            {
                if (!_finished)
                {
                    _cancellationTokenSource.Cancel();
                    // If the user is trying to close this form, then treat it the 
                    // same as if they had hit "Cancel".
                    e.Cancel = true;
                    return;
                }
                _windowShown = false;
            }
            base.OnFormClosing(e);
        }

        public int ProgressValue { get; set; }
        public string Message { get; set; }
        public CancellationToken CancellationToken { get { return _cancellationTokenSource.Token; } }

        private void timerUpdate_Tick(object sender, EventArgs e)
        {
            progressBar.Value = ProgressValue;
            labelMessage.Text = Message + (CancellationToken.IsCancellationRequested ? " (Cancelled)" : string.Empty);
        }
    }
}
