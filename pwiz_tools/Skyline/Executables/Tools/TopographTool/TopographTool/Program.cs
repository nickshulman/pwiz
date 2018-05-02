using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using TopographTool.Model;
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
            if (args.Length == 3)
            {
                var data = TopographData.Read(args[0], args[1], args[2]);
                form.Data = data;
            }
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
