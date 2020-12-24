using System.ComponentModel;
using Serilog;
using Serilog.Core;

namespace pwiz.Skyline.Model.EventLog
{
    public static class UiLog
    {
        private static Logger _logger;

        static UiLog()
        {
            var configuration = new LoggerConfiguration();
            configuration = configuration.WriteTo.Sink(SkylineEventLog.INSTANCE);

        }
        public static void LogAction(IComponent component, string action)
        {

        }
    }
}
