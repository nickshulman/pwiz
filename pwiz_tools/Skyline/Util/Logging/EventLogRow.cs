using System;
using System.Collections.Generic;
using System.IO;
using Serilog.Events;

namespace pwiz.Skyline.Util.Logging
{
    public class EventLogRow
    {
        private LogEvent _logEvent;
        private Lazy<IDictionary<string, string>> _properties;
        
        public EventLogRow(LogEvent logEvent)
        {
            _logEvent = logEvent;
            _properties = new Lazy<IDictionary<string, string>>(GetProperties);
        }

        public string Message
        {
            get
            {
                return _logEvent.RenderMessage();
            }
        }

        public LogEventLevel LogLevel
        {
            get
            {
                return _logEvent.Level;
            }
        }

        public DateTimeOffset TimeStamp
        {
            get
            {
                return _logEvent.Timestamp;
            }
        }

        public IDictionary<string, string> Properties
        {
            get { return _properties.Value; }
        }

        private IDictionary<string, string> GetProperties()
        {
            var dict = new Dictionary<string, string>();
            foreach (var param in _logEvent.Properties)
            {
                var stringWriter = new StringWriter();
                param.Value.Render(stringWriter);
                dict.Add(param.Key, stringWriter.ToString());
            }

            return dict;
        }

        public override string ToString()
        {
            return Message;
        }
    }
}
