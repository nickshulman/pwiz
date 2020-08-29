using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using Serilog.Core;

namespace pwiz.Skyline.Util.Logging
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
