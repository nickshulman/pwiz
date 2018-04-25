using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TopographTool.Ui;

namespace TopographTool
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Init();
            var form = new TopographForm();
            Application.Run(form);
        }

        public static void Init()
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += ThreadExceptionEventHandler;

        }

        private static void ThreadExceptionEventHandler(Object sender, ThreadExceptionEventArgs e)
        {
            Trace.TraceError("Unhandled exception on UI thread: {0}", e.Exception); // Not L10N
            var stackTrace = new StackTrace(1, true);
            string message = String.Join(Environment.NewLine,
                string.Format("An exception of type '{0}' occurred:", e.Exception.GetType().FullName),
                e.Exception.Message, stackTrace);
            MessageBox.Show(message);
        }
    }
}
