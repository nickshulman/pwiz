using System;
using System.Collections.Generic;
using System.Linq;
using pwiz.Common.Collections;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace pwiz.Skyline.Util.Logging
{
    public class SkylineEventLog : ILogEventSink
    {
        public static void Initialize()
        {
            var configuration = new Serilog.LoggerConfiguration().WriteTo.Console();
            configuration = configuration.WriteTo.Sink(new SkylineEventLog());
            Serilog.Log.Logger = configuration.CreateLogger();
        }
        private static LinkedList<LogEvent> _logEvents = new LinkedList<LogEvent>();
        public static int EventLogSize = 1000;
        public void Emit(LogEvent logEvent)
        {
            lock (_logEvents)
            {
                _logEvents.AddLast(logEvent);
                while (_logEvents.Count > EventLogSize)
                {
                    _logEvents.RemoveFirst();
                }

                NewEventsAvailable?.Invoke();
            }
        }

        public static event Action NewEventsAvailable;

        public static List<LogEvent> GetLogEvents()
        {
            lock (_logEvents)
            {
                return _logEvents.ToList();
            }
        }
    }
}
